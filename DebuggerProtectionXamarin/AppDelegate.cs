using Foundation;
using UIKit;
using System;
using System.Threading.Tasks;
using CoreGraphics;

namespace DebuggerProtectionXamarin;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        Console.WriteLine("AppDelegate: FinishedLaunching method called");
        
        Console.WriteLine("DebuggerDetector.PreventDebugging() called.");
        DebuggerDetector.PreventDebugging();
        // Explicitly call IsBeingDebugged and log the result
        bool isDebugged = DebuggerDetector.IsBeingDebugged();
        Console.WriteLine($"AppDelegate: DebuggerDetector.IsBeingDebugged() returned {isDebugged}");

        if (isDebugged)
        {
            Console.WriteLine("AppDelegate: Warning: Application is being debugged!");
            HandleDebuggerDetected();
        }
        else
        {
            Console.WriteLine("AppDelegate: No debugger detected, setting up normal UI");
            SetupUI();
        }

        return true;
    }

    private void SetupUI()
    {
        Console.WriteLine("AppDelegate: Setting up normal UI");
        Window = new UIWindow(UIScreen.MainScreen.Bounds);
        var viewController = new UIViewController();
        Window.RootViewController = viewController;

        var label = new UILabel(Window.Frame)
        {
            Text = "Hello, World!",
            TextAlignment = UITextAlignment.Center,
            TextColor = UIColor.Green,
            Font = UIFont.SystemFontOfSize(24)
        };
        viewController.View.AddSubview(label);

        Window.MakeKeyAndVisible();
        Console.WriteLine("AppDelegate: Normal UI setup complete");
    }

    private async void HandleDebuggerDetected()
    {
        Console.WriteLine("AppDelegate: HandleDebuggerDetected called");
        await UpdateUIForDebuggerDetection();

        // for (int i = 10; i > 0; i--)
        // {
        //     Console.WriteLine($"AppDelegate: Exiting in {i} seconds...");
        //     await Task.Delay(1000);
        // }

        // Console.WriteLine("AppDelegate: Exiting application due to debugger detection.");
        // Environment.Exit(1);
    }

    private Task UpdateUIForDebuggerDetection()
    {
        Console.WriteLine("AppDelegate: Updating UI for debugger detection");
        return InvokeOnMainThreadAsync(() =>
        {
            Window = new UIWindow(UIScreen.MainScreen.Bounds);
            var viewController = new UIViewController();
            Window.RootViewController = viewController;

            var alertLabel = new UILabel(Window.Frame)
            {
                Text = "Debugger Detected!\nExiting in 10 seconds...",
                TextAlignment = UITextAlignment.Center,
                TextColor = UIColor.Red,
                Font = UIFont.BoldSystemFontOfSize(20),
                Lines = 0,
                BackgroundColor = UIColor.Black
            };
            viewController.View.AddSubview(alertLabel);

            Window.MakeKeyAndVisible();
            Console.WriteLine("AppDelegate: Debugger detection UI setup complete");
        });
    }

    private Task InvokeOnMainThreadAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        InvokeOnMainThread(() =>
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
