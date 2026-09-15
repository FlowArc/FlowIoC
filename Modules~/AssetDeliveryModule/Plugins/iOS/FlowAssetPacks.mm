// FlowAssetPacks.mm - Apple-hosted managed Background Assets for FlowIoC's asset delivery module.
//
// Every callback into C# is made on the main queue. A string handed back to C# is strdup'ed here
// and freed there. Below iOS 26 every entry point answers that managed asset packs are not
// available, and the C# gateway turns that into a reason on the loading bar.
//
// Written against Apple's Background Assets reference (Objective-C) and unverified on a device.
// The selectors used: getAssetPackWithIdentifier:completionHandler:,
// assetPackIsAvailableLocallyWithIdentifier:, ensureLocalAvailabilityOfAssetPacks:completionHandler:,
// removeAssetPackWithIdentifier:completionHandler:, URLForPath:error:, downloadSize.

#import <Foundation/Foundation.h>
#import <BackgroundAssets/BackgroundAssets.h>

typedef void (*FlowAssetPacksStatusCallback)(int requestId, const char* pack, int status, long long totalBytes, long long downloadedBytes, const char* error);
typedef void (*FlowAssetPacksProgressCallback)(int requestId, long long totalBytes, long long downloadedBytes);
typedef void (*FlowAssetPacksDoneCallback)(int requestId, bool ok, const char* error);

static const int kFlowAssetPacksStatusOnDevice = 1;
static const int kFlowAssetPacksStatusMissing = 0;
static const int kFlowAssetPacksStatusError = -1;
static const char* kFlowAssetPacksUnsupported = "managed asset packs need iOS 26 or later";

API_AVAILABLE(ios(26.0))
@interface FlowAssetPacksDelegate : NSObject <BAManagedAssetPackDownloadDelegate>
@property (nonatomic) int requestId;
@property (nonatomic) FlowAssetPacksProgressCallback progress;
@property (nonatomic, strong) NSMutableDictionary<NSString*, NSNumber*>* totals;
@property (nonatomic, strong) NSMutableDictionary<NSString*, NSNumber*>* downloaded;
@end

@implementation FlowAssetPacksDelegate

- (instancetype)init {
    self = [super init];
    if (self) {
        _totals = [NSMutableDictionary new];
        _downloaded = [NSMutableDictionary new];
    }
    return self;
}

- (void)report {
    long long total = 0;
    long long done = 0;
    for (NSNumber* n in self.totals.allValues) total += n.longLongValue;
    for (NSNumber* n in self.downloaded.allValues) done += n.longLongValue;
    if (self.progress != NULL) self.progress(self.requestId, total, done);
}

- (void)downloadOfAssetPackBegan:(BAAssetPack*)assetPack {
    self.totals[assetPack.identifier] = @(assetPack.downloadSize);
    dispatch_async(dispatch_get_main_queue(), ^{ [self report]; });
}

- (void)downloadOfAssetPack:(BAAssetPack*)assetPack hasProgress:(NSProgress*)progress {
    self.totals[assetPack.identifier] = @(progress.totalUnitCount > 0 ? progress.totalUnitCount : assetPack.downloadSize);
    self.downloaded[assetPack.identifier] = @(progress.completedUnitCount);
    dispatch_async(dispatch_get_main_queue(), ^{ [self report]; });
}

- (void)downloadOfAssetPackPaused:(BAAssetPack*)assetPack {
}

- (void)downloadOfAssetPackFinished:(BAAssetPack*)assetPack {
    NSNumber* total = self.totals[assetPack.identifier] ?: @(assetPack.downloadSize);
    self.totals[assetPack.identifier] = total;
    self.downloaded[assetPack.identifier] = total;
    dispatch_async(dispatch_get_main_queue(), ^{ [self report]; });
}

- (void)downloadOfAssetPack:(BAAssetPack*)assetPack failedWithError:(NSError*)error {
}

@end

static id gFlowAssetPacksDelegate = nil;

static const char* FlowAssetPacksCopy(NSString* text) {
    return text == nil ? NULL : strdup(text.UTF8String);
}

extern "C" {

bool FlowAssetPacks_IsSupported() {
    if (@available(iOS 26.0, *)) return true;
    return false;
}

void FlowAssetPacks_GetStatus(const char* pack, int requestId, FlowAssetPacksStatusCallback callback) {
    NSString* identifier = [NSString stringWithUTF8String:pack];
    NSString* packCopy = [identifier copy];

    if (@available(iOS 26.0, *)) {
        BAAssetPackManager* manager = [BAAssetPackManager sharedManager];
        [manager getAssetPackWithIdentifier:identifier completionHandler:^(BAAssetPack* assetPack, NSError* error) {
            dispatch_async(dispatch_get_main_queue(), ^{
                if (assetPack == nil) {
                    callback(requestId, packCopy.UTF8String, kFlowAssetPacksStatusError, 0, 0, error.localizedDescription.UTF8String);
                    return;
                }
                BOOL local = [manager assetPackIsAvailableLocallyWithIdentifier:identifier];
                long long size = (long long) assetPack.downloadSize;
                callback(requestId, packCopy.UTF8String, local ? kFlowAssetPacksStatusOnDevice : kFlowAssetPacksStatusMissing, size, local ? size : 0, NULL);
            });
        }];
        return;
    }

    callback(requestId, pack, kFlowAssetPacksStatusError, 0, 0, kFlowAssetPacksUnsupported);
}

void FlowAssetPacks_Ensure(const char* packsSeparatedByComma, int requestId, FlowAssetPacksProgressCallback progress, FlowAssetPacksDoneCallback done) {
    NSArray<NSString*>* identifiers = [[NSString stringWithUTF8String:packsSeparatedByComma] componentsSeparatedByString:@","];

    if (@available(iOS 26.0, *)) {
        BAAssetPackManager* manager = [BAAssetPackManager sharedManager];
        FlowAssetPacksDelegate* delegate = [FlowAssetPacksDelegate new];
        delegate.requestId = requestId;
        delegate.progress = progress;
        gFlowAssetPacksDelegate = delegate;
        manager.delegate = delegate;

        NSMutableSet<BAAssetPack*>* packs = [NSMutableSet new];
        dispatch_group_t group = dispatch_group_create();
        __block NSError* lookupError = nil;

        for (NSString* identifier in identifiers) {
            dispatch_group_enter(group);
            [manager getAssetPackWithIdentifier:identifier completionHandler:^(BAAssetPack* assetPack, NSError* error) {
                @synchronized(packs) {
                    if (assetPack != nil) [packs addObject:assetPack];
                    else lookupError = error;
                }
                dispatch_group_leave(group);
            }];
        }

        dispatch_group_notify(group, dispatch_get_main_queue(), ^{
            if (lookupError != nil) {
                manager.delegate = nil;
                gFlowAssetPacksDelegate = nil;
                done(requestId, false, lookupError.localizedDescription.UTF8String);
                return;
            }
            [manager ensureLocalAvailabilityOfAssetPacks:packs completionHandler:^(NSError* error) {
                dispatch_async(dispatch_get_main_queue(), ^{
                    manager.delegate = nil;
                    gFlowAssetPacksDelegate = nil;
                    done(requestId, error == nil, error == nil ? NULL : error.localizedDescription.UTF8String);
                });
            }];
        });
        return;
    }

    done(requestId, false, kFlowAssetPacksUnsupported);
}

void FlowAssetPacks_Remove(const char* pack, int requestId, FlowAssetPacksDoneCallback done) {
    NSString* identifier = [NSString stringWithUTF8String:pack];

    if (@available(iOS 26.0, *)) {
        [[BAAssetPackManager sharedManager] removeAssetPackWithIdentifier:identifier completionHandler:^(NSError* error) {
            dispatch_async(dispatch_get_main_queue(), ^{
                done(requestId, error == nil, error == nil ? NULL : error.localizedDescription.UTF8String);
            });
        }];
        return;
    }

    done(requestId, false, kFlowAssetPacksUnsupported);
}

const char* FlowAssetPacks_PathForFile(const char* relativePath) {
    if (@available(iOS 26.0, *)) {
        NSError* error = nil;
        NSURL* url = [[BAAssetPackManager sharedManager] URLForPath:[NSString stringWithUTF8String:relativePath] error:&error];
        if (url == nil) return NULL;
        return FlowAssetPacksCopy(url.path);
    }
    return NULL;
}

}
