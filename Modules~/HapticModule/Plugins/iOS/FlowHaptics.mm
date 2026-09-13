// FlowIoC HapticModule - the preset side of haptics on iOS.
//
// UIKit's own generators, one per style, prepared before each trigger. This is what a preset is
// on iOS; Core Haptics is not involved. The numbers are HapticPreset's and never move.
// Compiled with -fobjc-arc (set on the file's plugin importer), so the statics release on nil.

#import <UIKit/UIKit.h>

static UISelectionFeedbackGenerator *flowSelection = nil;
static UINotificationFeedbackGenerator *flowNotification = nil;
static UIImpactFeedbackGenerator *flowLight = nil;
static UIImpactFeedbackGenerator *flowMedium = nil;
static UIImpactFeedbackGenerator *flowHeavy = nil;
static UIImpactFeedbackGenerator *flowRigid = nil;
static UIImpactFeedbackGenerator *flowSoft = nil;

static void FlowHapticsImpact(UIImpactFeedbackGenerator *generator)
{
    if (generator == nil) return;
    [generator prepare];
    [generator impactOccurred];
}

static void FlowHapticsNotify(UINotificationFeedbackType type)
{
    if (flowNotification == nil) return;
    [flowNotification prepare];
    [flowNotification notificationOccurred:type];
}

extern "C" bool FlowHapticsInitialize(void)
{
    flowSelection = [[UISelectionFeedbackGenerator alloc] init];
    flowNotification = [[UINotificationFeedbackGenerator alloc] init];
    flowLight = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
    flowMedium = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
    flowHeavy = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];

    if (@available(iOS 13, *))
    {
        flowRigid = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleRigid];
        flowSoft = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleSoft];
    }
    else
    {
        // Rigid and Soft arrived with iOS 13; below it the nearest of the older three stands in.
        flowRigid = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
        flowSoft = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
    }

    return flowSelection != nil && flowNotification != nil && flowLight != nil && flowMedium != nil
        && flowHeavy != nil && flowRigid != nil && flowSoft != nil;
}

extern "C" void FlowHapticsTrigger(int preset)
{
    switch (preset)
    {
        case 0: // Selection
            if (flowSelection == nil) return;
            [flowSelection prepare];
            [flowSelection selectionChanged];
            break;
        case 1: FlowHapticsNotify(UINotificationFeedbackTypeSuccess); break; // Success
        case 2: FlowHapticsNotify(UINotificationFeedbackTypeWarning); break; // Warning
        case 3: FlowHapticsNotify(UINotificationFeedbackTypeError); break;   // Failure
        case 4: FlowHapticsImpact(flowLight); break;                          // LightImpact
        case 5: FlowHapticsImpact(flowMedium); break;                         // MediumImpact
        case 6: FlowHapticsImpact(flowHeavy); break;                          // HeavyImpact
        case 7: FlowHapticsImpact(flowRigid); break;                          // RigidImpact
        case 8: FlowHapticsImpact(flowSoft); break;                           // SoftImpact
        default: break;                                                       // None
    }
}

extern "C" void FlowHapticsRelease(void)
{
    flowSelection = nil;
    flowNotification = nil;
    flowLight = nil;
    flowMedium = nil;
    flowHeavy = nil;
    flowRigid = nil;
    flowSoft = nil;
}
