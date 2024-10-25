using Foundation;
using UIKit;
using System;
using CoreGraphics;

namespace DebuggerProtectionXamarin;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        Console.WriteLine("AppDelegate: FinishedLaunching method called");

        DebuggerDetector.LogAppOpening();
        DebuggerDetector.PreventDebugging();
        DebuggerDetector.StartContinuousDebuggerChecks(() =>
        {
            Console.WriteLine("AppDelegate: Debugger detected! Exiting...");
            Environment.Exit(1);
        });

        // Create a simple "Hello, World!" UI
        Console.WriteLine("AppDelegate: Setting up UI");
        Window = new UIWindow(UIScreen.MainScreen.Bounds);
        Console.WriteLine($"AppDelegate: Window created with bounds: {Window.Bounds}");

        var viewController = new UIViewController();
        Window.RootViewController = viewController;
        Console.WriteLine("AppDelegate: RootViewController set");

        if (viewController.View != null)
        {
            viewController.View.BackgroundColor = UIColor.White;
            Console.WriteLine("AppDelegate: View background color set to white");

            var label = new UILabel(new CGRect(0, 0, Window.Frame.Width, Window.Frame.Height))
            {
                Text = "Hello, World!",
                TextAlignment = UITextAlignment.Center,
                TextColor = UIColor.Black,
                Font = UIFont.SystemFontOfSize(24)
            };
            Console.WriteLine($"AppDelegate: Label created with frame: {label.Frame}");

            viewController.View.AddSubview(label);
            Console.WriteLine("AppDelegate: Label added to view");
        }
        else
        {
            Console.WriteLine("AppDelegate: Warning - viewController.View is null");
        }

        Window.MakeKeyAndVisible();
        Console.WriteLine("AppDelegate: Window made key and visible");

        return true;
    }
}
