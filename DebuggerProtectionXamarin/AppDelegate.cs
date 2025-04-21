// DebuggerProtectionXamarin/AppDelegate.cs
using Foundation;
using UIKit;
using System;
using System.Threading.Tasks;
using CoreGraphics;
using System.Collections.Generic; // Needed for List
using System.Linq; // Needed for Select

namespace DebuggerProtectionXamarin;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        Console.WriteLine("AppDelegate: FinishedLaunching method called");

        // --- Debugger Detection ---
        Console.WriteLine("DebuggerDetector.PreventDebugging() called.");
        DebuggerDetector.PreventDebugging();
        bool isDebugged = DebuggerDetector.IsBeingDebugged();
        Console.WriteLine($"AppDelegate: DebuggerDetector.IsBeingDebugged() returned {isDebugged}");

        // --- Integrity Checks ---
        Console.WriteLine("Performing Integrity Checks...");
        var integrityChecks = new List<FileIntegrityCheck>
        {
            // IMPORTANT: Replace with your actual Bundle ID from Info.plist
            new FileIntegrityCheck {
                Type = FileIntegrityCheckType.BundleId,
                ExpectedValue = "com.appknox.DebuggerProtectionXamarin"
            },
            // IMPORTANT: Replace with the actual SHA256 hash of your embedded.mobileprovision file
            new FileIntegrityCheck {
                Type = FileIntegrityCheckType.MobileProvision,
                ExpectedValue = "YOUR_EXPECTED_MOBILEPROVISION_SHA256_HASH_HERE" // e.g., "a1b2c3d4..."
            }
        };

        FileIntegrityCheckResult integrityResult = IntegrityChecker.AmITampered(integrityChecks);
        Console.WriteLine($"AppDelegate: IntegrityChecker.AmITampered() returned IsTampered={integrityResult.IsTampered}");

        // --- Handle Results ---
        if (isDebugged || integrityResult.IsTampered)
        {
            Console.WriteLine("AppDelegate: Warning: Debugger detected OR Integrity check failed!");
            if (isDebugged) Console.WriteLine("Reason: Debugger Attached");
            if (integrityResult.IsTampered)
            {
                Console.WriteLine("Reason: Integrity Check Failed. Failed checks:");
                foreach (var failedCheck in integrityResult.FailedChecks)
                {
                    Console.WriteLine($"- {failedCheck.Type}: {failedCheck}");
                }
            }
            HandleIssueDetected(isDebugged, integrityResult); // Pass results to handler
        }
        else
        {
            Console.WriteLine("AppDelegate: No debugger detected and integrity checks passed, setting up normal UI");
            SetupUI();
        }

        return true;
    }

    private void SetupUI()
    {
        Console.WriteLine("AppDelegate: Setting up normal UI");
        Window = new UIWindow(UIScreen.MainScreen.Bounds);
        var viewController = new UIViewController();
        viewController.View.BackgroundColor = UIColor.White; // Added for visibility
        Window.RootViewController = viewController;

        var label = new UILabel(new CGRect(0, 0, Window.Frame.Width, 100)) // Adjusted frame
        {
            Center = Window.Center, // Center the label
            Text = "Hello, World! Checks Passed.",
            TextAlignment = UITextAlignment.Center,
            TextColor = UIColor.Green,
            Font = UIFont.SystemFontOfSize(24)
        };
        viewController.View.AddSubview(label);

        Window.MakeKeyAndVisible();
        Console.WriteLine("AppDelegate: Normal UI setup complete");
    }

    // Modified to accept reasons and display more info
    private async void HandleIssueDetected(bool debuggerDetected, FileIntegrityCheckResult integrityResult)
    {
        Console.WriteLine("AppDelegate: HandleIssueDetected called");

        // Construct the message
        var messageBuilder = new System.Text.StringBuilder();
        messageBuilder.AppendLine("Security Alert!");
        if (debuggerDetected)
        {
            messageBuilder.AppendLine("- Debugger Detected!");
        }
        if (integrityResult.IsTampered)
        {
            messageBuilder.AppendLine("- Integrity Check Failed:");
            string failedCheckDetails = string.Join(", ", integrityResult.FailedChecks.Select(fc => fc.Type.ToString()));
            messageBuilder.AppendLine($"  ({failedCheckDetails})");
        }
        // messageBuilder.AppendLine("\nExiting application..."); // Modify exit behavior as needed

        await UpdateUIForIssueDetection(messageBuilder.ToString());

        // --- Choose your reaction ---
        // Option 1: Exit after delay (as in your original code)
        // Console.WriteLine("AppDelegate: Exiting application due to security issue.");
        // await Task.Delay(5000); // Reduced delay for testing
        // Environment.Exit(1);

        // Option 2: Just show the warning and let the app run (for testing/debugging the detection)
         Console.WriteLine("AppDelegate: Displaying warning but not exiting.");

        // Option 3: Implement more sophisticated response (e.g., disable features, notify server)
    }

    // Modified to accept a custom message
    private Task UpdateUIForIssueDetection(string message)
    {
        Console.WriteLine("AppDelegate: Updating UI for issue detection");
        return InvokeOnMainThreadAsync(() =>
        {
            Window = new UIWindow(UIScreen.MainScreen.Bounds);
            var viewController = new UIViewController();
            Window.RootViewController = viewController;

            var alertLabel = new UILabel(new CGRect(20, 100, Window.Frame.Width - 40, Window.Frame.Height - 200)) // Adjusted frame
            {
                Text = message,
                TextAlignment = UITextAlignment.Center,
                TextColor = UIColor.Red,
                Font = UIFont.BoldSystemFontOfSize(20),
                Lines = 0, // Allow multiple lines
                LineBreakMode = UILineBreakMode.WordWrap, // Wrap text
                BackgroundColor = UIColor.Black
            };
            viewController.View.AddSubview(alertLabel);

            Window.MakeKeyAndVisible();
            Console.WriteLine("AppDelegate: Issue detection UI setup complete");
        });
    }

    private Task InvokeOnMainThreadAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        NSRunLoop.Main.BeginInvokeOnMainThread(() => // Use NSRunLoop.Main for modern iOS
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AppDelegate: Exception in InvokeOnMainThreadAsync: {ex.Message}");
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }
}