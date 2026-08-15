/*
 * version_shim.c
 *
 * Problem: FM26 (fm.app) embeds a non-standard Unity version string in
 * Contents/Resources/Data/globalgamemanagers: "6000.0.52f1-fm26-05f1"
 * (Sports Interactive custom build suffix). BepInEx.Unity.Common.UnityInfo.
 * DetermineVersion() reads this string and calls AssetRipper.Primitives'
 * UnityVersion.Parse() on it, which throws on the "-fm26-05f1" suffix and
 * falls back to UnityVersion 0.0.0a0. That fake version then breaks the
 * Il2CppInterop base-library download (404, URL built from the version)
 * and Il2CppInterop.Runtime.UnityVersionHandler (no struct handler
 * registered for 0.0.0 => "No handler" exception in the Preloader, before
 * any plugin loads).
 *
 * There is no BepInEx config key, env var, or CLI flag to override the
 * detected Unity version (verified by decompiling BepInEx.Unity.Common.dll:
 * UnityInfo.SetRuntimeUnityVersion exists but is `internal`, and is never
 * reachable before the failure -- Il2CppInteropManager.Initialize() runs
 * before even patcher plugins are loaded, per BepInEx.Unity.IL2CPP.dll's
 * decompiled Preloader.Run()). Writing a corrected file inside
 * fm.app/Contents/... is blocked by macOS "App Management" protection
 * (EPERM, not a Unix permission problem -- confirmed with normal
 * rwxr-xr-x, no ACL/flags, read-write volume).
 *
 * Fix implemented here: a dyld symbol-interposition shim, loaded via
 * DYLD_INSERT_LIBRARIES (already confirmed to work for this game under
 * Rosetta), that intercepts open/read/pread/lseek/close and, for the
 * single, first-ever open() of a path ending in "/globalgamemanagers",
 * patches exactly one in-memory byte as it flows through the read buffer:
 * the '-' (0x2D) right after "6000.0.52f1" at absolute file offset 0x3B
 * (59, confirmed via xxd), turned into a NUL (0x00). This truncates the
 * string BepInEx reads from "6000.0.52f1-fm26-05f1" to "6000.0.52f1",
 * which UnityVersion.Parse accepts. Nothing is ever written to disk; the
 * file on disk is untouched.
 *
 * Uses the standard macOS __DATA,__interpose section mechanism (not a
 * plain same-named symbol override), which is the only technique
 * guaranteed to work under the default two-level namespace binding that
 * libSystem callers use. Each replacement calls the original function BY
 * NAME directly (the documented Apple DYLD_INTERPOSE idiom) rather than
 * via dlsym(RTLD_NEXT, ...): testing on this machine showed
 * dlsym(RTLD_NEXT, "close") resolving back to this library's own
 * replacement (apparently due to how the dyld shared cache interacts with
 * RTLD_NEXT); since the compiler tail-call-optimized that call, it turned
 * into an infinite loop the moment libSystem's own bootstrap (malloc init
 * -> feature flags -> close()) ran, which happens before this library's
 * constructor even fires. Calling the original symbol by name does not
 * have this problem
 * problem: dyld's interpose rewriting only redirects OTHER images'
 * references to the interposee, never the interposing image's own direct
 * call to the same name, so it safely reaches the real implementation
 * with no dlsym involved and no ordering dependency on constructors.
 *
 * Safety/scope:
 *   - Only a path whose filename is exactly "globalgamemanagers" is
 *     tracked (suffix match on the path passed to open()).
 *   - The byte is only overwritten if it currently equals the expected
 *     0x2D ('-'); if the game/BepInEx ever change and the byte differs,
 *     the shim leaves it alone rather than guessing.
 *   - After the first close() of a tracked fd, further opens of matching
 *     paths are no longer tracked/patched (global one-shot latch). This
 *     is meant to catch only BepInEx's very early version probe (which
 *     happens during Doorstop/Preloader bootstrap, before Unity's own
 *     engine has started reading its asset files) and leave any later,
 *     legitimate reads of the same file by the Unity engine itself
 *     completely untouched.
 *   - No file is ever written; no bytes other than the single targeted
 *     offset are ever modified, and only in memory buffers returned to
 *     the caller.
 *
 * Build: see build.sh in this directory (must be built for x86_64, since
 * FM26 on this Mac runs translated under Rosetta 2 via
 * `arch -x86_64 ./run_bepinex.sh`).
 */

#include <fcntl.h>
#include <pthread.h>
#include <stdarg.h>
#include <stdatomic.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/types.h>

#define TARGET_SUFFIX "/globalgamemanagers"
#define PATCH_OFFSET   59      /* 0x3B: byte right after "6000.0.52f1" */
#define ORIG_BYTE      0x2D    /* '-' */
#define PATCHED_BYTE   0x00    /* NUL terminator */

#define MAX_FDS 4096

typedef struct {
    _Atomic int active; /* 1 while this fd is the tracked globalgamemanagers fd */
    off_t pos;          /* tracked current file offset (best effort, for plain read()) */
} fd_state_t;

static fd_state_t g_fds[MAX_FDS];
static atomic_int g_already_consumed = 0; /* one-shot latch: only ever patch the first open/close cycle */
static pthread_mutex_t g_lock = PTHREAD_MUTEX_INITIALIZER;

static void shim_log(const char *fmt, ...) {
    va_list ap;
    va_start(ap, fmt);
    fprintf(stderr, "[version_shim] ");
    vfprintf(stderr, fmt, ap);
    fprintf(stderr, "\n");
    va_end(ap);
    fflush(stderr);
}

__attribute__((constructor))
static void version_shim_init(void) {
    for (int i = 0; i < MAX_FDS; i++) {
        g_fds[i].active = 0;
        g_fds[i].pos = 0;
    }
    shim_log("loaded, targeting offset %d in paths ending with \"%s\" (one-shot)", PATCH_OFFSET, TARGET_SUFFIX);
}

static int path_matches(const char *path) {
    if (!path) return 0;
    size_t plen = strlen(path);
    size_t slen = strlen(TARGET_SUFFIX);
    if (plen < slen) return 0;
    return strcmp(path + (plen - slen), TARGET_SUFFIX) == 0;
}

static void patch_buffer_if_needed(int fd, void *buf, ssize_t nread, off_t range_start) {
    if (nread <= 0) return;
    if (fd < 0 || fd >= MAX_FDS) return;
    if (!g_fds[fd].active) return;

    off_t range_end = range_start + nread; /* exclusive */
    if (PATCH_OFFSET >= range_start && PATCH_OFFSET < range_end) {
        unsigned char *bytes = (unsigned char *)buf;
        size_t idx = (size_t)(PATCH_OFFSET - range_start);
        unsigned char before = bytes[idx];
        if (before == ORIG_BYTE) {
            bytes[idx] = PATCHED_BYTE;
            shim_log("patched fd=%d at absolute offset %d: 0x%02X -> 0x%02X", fd, PATCH_OFFSET, before, PATCHED_BYTE);
        } else {
            shim_log("fd=%d byte at offset %d was 0x%02X (expected 0x%02X) -- left untouched", fd, PATCH_OFFSET, before, ORIG_BYTE);
        }
    }
}

/* --- replacement implementations (registered via __interpose below) --- */

static int my_open(const char *path, int flags, ...) {
    mode_t mode = 0;
    if (flags & O_CREAT) {
        va_list ap;
        va_start(ap, flags);
        mode = (mode_t)va_arg(ap, int);
        va_end(ap);
    }

    int fd = open(path, flags, mode);

#ifdef VERSION_SHIM_TRACE_ALL_OPENS
    shim_log("open(\"%s\") -> fd=%d", path ? path : "(null)", fd);
#endif

    if (fd >= 0 && fd < MAX_FDS && !atomic_load(&g_already_consumed) && path_matches(path)) {
        pthread_mutex_lock(&g_lock);
        if (!atomic_load(&g_already_consumed)) {
            g_fds[fd].active = 1;
            g_fds[fd].pos = 0;
            shim_log("tracking fd=%d for path \"%s\"", fd, path);
        }
        pthread_mutex_unlock(&g_lock);
    }
    return fd;
}

static int my_close(int fd) {
    if (fd >= 0 && fd < MAX_FDS && g_fds[fd].active) {
        pthread_mutex_lock(&g_lock);
        g_fds[fd].active = 0;
        atomic_store(&g_already_consumed, 1);
        shim_log("fd=%d closed, disabling further tracking (one-shot consumed)", fd);
        pthread_mutex_unlock(&g_lock);
    }
    return close(fd);
}

static off_t my_lseek(int fd, off_t offset, int whence) {
    off_t result = lseek(fd, offset, whence);
    if (result >= 0 && fd >= 0 && fd < MAX_FDS && g_fds[fd].active) {
        g_fds[fd].pos = result;
    }
    return result;
}

static ssize_t my_read(int fd, void *buf, size_t count) {
    ssize_t n = read(fd, buf, count);
    if (n > 0 && fd >= 0 && fd < MAX_FDS && g_fds[fd].active) {
        off_t start = g_fds[fd].pos;
        patch_buffer_if_needed(fd, buf, n, start);
        g_fds[fd].pos = start + n;
    }
    return n;
}

static ssize_t my_pread(int fd, void *buf, size_t count, off_t offset) {
    ssize_t n = pread(fd, buf, count, offset);
    if (n > 0 && fd >= 0 && fd < MAX_FDS && g_fds[fd].active) {
        patch_buffer_if_needed(fd, buf, n, offset);
    }
    return n;
}

/* --- dyld interpose table: __DATA,__interpose is required (rather than
 * just exporting same-named symbols) for this to reliably override calls
 * made through two-level-namespace bindings, which is the default for
 * anything linked against libSystem. --- */

#define DYLD_INTERPOSE(_replacement, _replacee) \
    __attribute__((used)) static struct { const void *replacement; const void *replacee; } \
    _interpose_##_replacee __attribute__((section("__DATA,__interpose"))) = \
    { (const void *)(unsigned long)&(_replacement), (const void *)(unsigned long)&(_replacee) };

DYLD_INTERPOSE(my_open, open)
DYLD_INTERPOSE(my_close, close)
DYLD_INTERPOSE(my_lseek, lseek)
DYLD_INTERPOSE(my_read, read)
DYLD_INTERPOSE(my_pread, pread)
