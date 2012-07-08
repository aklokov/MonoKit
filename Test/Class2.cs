//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file=".cs" company="sgmunn">
//    (c) sgmunn 2012  
//
//    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//    documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
//    the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
//    to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//    The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
//    the Software.
//
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//    THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
//    CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
//    IN THE SOFTWARE.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------
//
using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Diagnostics;
using MonoTouch.CoreGraphics;
using MonoTouch.ObjCRuntime;

namespace Test
{
    public enum ViewDeckPanningMode
    {
        NoPanning,
        FullViewPanning,
        NavigationBarPanning,
        PanningViewPanning
    }

    public enum CenterHiddenInteractivity
    {
        UserInteractive,
        NotUserInteractive,
        NotUserInteractiveWithTapToClose,
        NotUserInteractiveWithTapToCloseBouncing
    }

    public enum ViewDeckNavigationControllerBehavior
    {
        Contained,
        Integrated
    }

    public enum ViewDeckRotationBehavior
    {
        KeepsLedgeSizes,
        KeepsViewSizes
    }

    public class ViewDeckController : UIViewController
    {
        private readonly List<UIGestureRecognizer> panners;

        private CenterHiddenInteractivity centerHiddenInteractivity;
        private bool viewAppeared;
        private bool resizesCenterView;
        private bool automaticallyUpdateTabBarItems;

        private UIViewController slidingController;

        private float originalShadowRadius;
        private SizeF originalShadowOffset;
        private UIColor originalShadowColor;
        private float originalShadowOpacity;

        private UIView referenceView;
        private UIBezierPath originalShadowPath;
        private UIView centerView;
        private UIButton centerTapper;

        private float maxLedge;
        private float offset;
        private float preRotationWidth;
        private float preRotationCenterWidth;
        private float leftWidth;
        private float rightWidth;
        private float panOrigin;


        private UIViewController _centerController;
        private UIViewController _leftController;
        private UIViewController _rightController;
        private float _rightLedge;
        private float _leftLedge;
        private ViewDeckNavigationControllerBehavior _navigationControllerBehavior;
        private ViewDeckPanningMode _panningMode;
        private UIView _panningView;

        public ViewDeckController(UIViewController centerController)
        {
            this.panners = new List<UIGestureRecognizer>();
            this.Enabled = true;
            this.Elastic = true;

// ??             this.originalShadowColor = UIColor.Clear;

            this.RotationBehavior = ViewDeckRotationBehavior.KeepsLedgeSizes;

            this.PanningMode = ViewDeckPanningMode.FullViewPanning;
            this.centerHiddenInteractivity = CenterHiddenInteractivity.UserInteractive;

            this.LeftLedge = 44;
            this.RightLedge = 44;
        
            this.CenterController = centerController;
        }


        public ViewDeckController(UIViewController centerController, UIViewController leftController) : this(centerController)
        {
            this.LeftController = leftController;
        }

        public ViewDeckController(UIViewController centerController, UIViewController leftController, UIViewController rightController) : this(centerController)
        {
            this.LeftController = leftController;
            this.RightController = rightController;
        }

        #region Public Properties

        /// <summary>
        /// </summary>
        public UIViewController CenterController
        {
            get
            {
                return this._centerController;
            }

            set
            {
                this.SetCenterController(value);
            }
        }

        /// <summary>
        /// </summary>
        public UIViewController LeftController
        {
            get
            {
                return this._leftController;
            }

            set
            {
                this.SetLeftController(value);
            }
        }

        /// <summary>
        /// </summary>
        public UIViewController RightController
        {
            get
            {
                return this._rightController;
            }

            set
            {
                this.SetRightController(value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the view deck is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the deck can be stretched past the ledges.
        /// </summary>
        public bool Elastic { get; set; }

        /// <summary>
        /// </summary>
        public float RightLedge
        {
            get
            {
                return this._rightLedge;
            }

            set
            {
                this.SetRightLedge(value);
            }
        }

        /// <summary>
        /// </summary>
        public float LeftLedge
        {
            get
            {
                return this._leftLedge;
            }

            set
            {
                this.SetLeftLedge(value);
            }
        }

        /// <summary>
        /// </summary>
        public ViewDeckNavigationControllerBehavior NavigationControllerBehavior
        {
            get
            {
                return this._navigationControllerBehavior;
            }

            set
            {
                if (this.viewAppeared) 
                {
                    throw new InvalidOperationException("Cannot set navigationcontroller behavior when the view deck is already showing.");
                }

                this._navigationControllerBehavior = value;
            }
        }

        /// <summary>
        /// </summary>
        public ViewDeckPanningMode PanningMode
        {
            get
            {
                return this._panningMode;
            }

            set
            {
                if (this.viewAppeared) 
                {
                    this.removePanners();
                    this._panningMode = value;
                    this.addPanners();
                }
                else
                {
                    this._panningMode = value;
                }

            }
        }

        /// <summary>
        /// </summary>
        public UIView PanningView
        {
            get
            {
                return this._panningView;
            }

            set
            {
                if (this._panningView != value) 
                {
                    // todo: dispose ??
                    //II_RELEASE(_panningView);
                    this._panningView = value;
                    //II_RETAIN(_panningView);
                    
                    if (this.viewAppeared && this.PanningMode == ViewDeckPanningMode.PanningViewPanning)
                    {
                        this.addPanners();
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        public ViewDeckRotationBehavior RotationBehavior { get; set; }


        #endregion

        #region Private Properties

        /// <summary>
        /// </summary>
        private UIView SlidingControllerView 
        {
            get
            {
                if (this.NavigationController != null && this.NavigationControllerBehavior == ViewDeckNavigationControllerBehavior.Integrated) 
                {
                    return this.slidingController.View;
                }
                else 
                {
                    return this.centerView;
                }
            }
        }
        
        /// <summary>
        /// </summary>
        private bool LeftControllerIsClosed 
        {
            get
            {
                return this.LeftController == null || this.SlidingControllerView.Frame.GetMinX() <= 0;
            }
        }

        /// <summary>
        /// </summary>
        private bool RightControllerIsClosed 
        {
            get
            {
                return this.RightController == null || this.SlidingControllerView.Frame.GetMaxX() >= this.referenceBounds.Size.Width;
            }
        }

        /// <summary>
        /// </summary>
        private bool LeftControllerIsOpen 
        {
            get
            {
                return this.LeftController != null && this.SlidingControllerView.Frame.GetMinX() < this.referenceBounds.Size.Width 
                    && this.SlidingControllerView.Frame.GetMinX() >= this.RightLedge;
            }
        }

        /// <summary>
        /// </summary>
        private bool RightControllerIsOpen 
        {
            get
            {
            return this.RightController != null && this.SlidingControllerView.Frame.GetMaxX() < this.referenceBounds.Size.Width 
                    && this.SlidingControllerView.Frame.GetMaxX() >= this.LeftLedge;
            }
        }


        #endregion

        private void CleanUp()
        {
            this.originalShadowRadius = 0;
            this.originalShadowOpacity = 0;
            this.originalShadowColor = null;
            this.originalShadowOffset = SizeF.Empty;
            this.originalShadowPath = null;
            
            this.slidingController = null;
            this.referenceView = null;
            this.centerView = null;
            this.centerTapper = null;
        }

        private void Dealloc()
        {
            this.CleanUp();
            
//            this.centerController.viewDeckController = null;
            this.CenterController = null;

            if (this.LeftController != null)
            {
//                this.leftController.viewDeckController = null;
                this.LeftController = null;
            }

            if (this.RightController != null)
            {
//                this.rightController.viewDeckController = null;
                this.RightController = null;
            }

            this.panners.Clear();
        }


        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();

            this.CenterController.DidReceiveMemoryWarning();

            if (this.LeftController != null)
            {
                this.LeftController.DidReceiveMemoryWarning();
            }

            if (this.RightController != null)
            {
                this.RightController.DidReceiveMemoryWarning();
            }
        }

        private List<UIViewController> controllers()
        {
            var result = new List<UIViewController>();

            result.Add(this.CenterController);

            if (this.LeftController != null)
            {
                result.Add(this.LeftController);
            }

            if (this.RightController != null)
            {
                result.Add(this.RightController);
            }

            return result;
        }

        private RectangleF referenceBounds
        {
            get
            {
                if (this.referenceView != null)
                {
                    return this.referenceView.Bounds;
                }

                return RectangleF.Empty;
            }
        }

        private float relativeStatusBarHeight
        {
            get
            {
                if (!this.referenceView.GetType().IsSubclassOf(typeof(UIWindow)))
                {
                    return 0;
                }   

                return this.statusBarHeight;
            }
        }

        private float statusBarHeight 
        {
            get
            {
                switch (UIApplication.SharedApplication.StatusBarOrientation)
                {
                    case UIInterfaceOrientation.LandscapeLeft:
                    case UIInterfaceOrientation.LandscapeRight:
                        return UIApplication.SharedApplication.StatusBarFrame.Width;
                    default:
                        return UIApplication.SharedApplication.StatusBarFrame.Height;
                }
            }
        }

        private static RectangleF II_RectangleFShrink(RectangleF rect, float width, float height)
        {
            return new RectangleF(rect.X, rect.Y, rect.Width - width, rect.Height - height);
        }

        private RectangleF centerViewBounds 
        {
            get
            {
                if (this.NavigationControllerBehavior == ViewDeckNavigationControllerBehavior.Contained)
                    return this.referenceBounds;
            
                return II_RectangleFShrink(this.referenceBounds, 
                                           0, 
                                           this.relativeStatusBarHeight + 
                                           (this.NavigationController.NavigationBarHidden ? 0 : this.NavigationController.NavigationBar.Frame.Size.Height));
            }
        }

        private static RectangleF II_RectangleFOffsetTopAndShrink(RectangleF rect, float offset)
        {
            return new RectangleF(rect.X, rect.Y, rect.Width, rect.Height - offset);
        }

        private RectangleF sideViewBounds 
        {
            get
            {
                if (this.NavigationControllerBehavior == ViewDeckNavigationControllerBehavior.Contained)
                    return this.referenceBounds;
            
                return II_RectangleFOffsetTopAndShrink(this.referenceBounds, this.relativeStatusBarHeight);
            }
        }

        private float limitOffset(float offset) 
        {
            if (this.LeftController != null && this.RightController != null) 
                return offset;
            
            if (this.LeftController != null && this.maxLedge > 0) 
            {
                float left = this.referenceBounds.Size.Width - this.maxLedge;
                offset = Math.Max(offset, left);
            }
            else if (this.RightController != null && this.maxLedge > 0) 
            {
                float right = this.maxLedge - this.referenceBounds.Size.Width;
                offset = Math.Min(offset, right);
            }
            
            return offset;
        }

        private RectangleF slidingRectForOffset(float offset) 
        {
            offset = this.limitOffset(offset);

            var sz = this.slidingSizeForOffset(offset);

            return new RectangleF(this.resizesCenterView && offset < 0 ? 0 : offset, 0, sz.Width, sz.Height);
        }

        private SizeF slidingSizeForOffset(float offset) 
        {
            if (!this.resizesCenterView) 
                return this.referenceBounds.Size;
            
            offset = this.limitOffset(offset);

            if (offset < 0) 
                return new SizeF(this.centerViewBounds.Size.Width + offset, this.centerViewBounds.Size.Height);
            
            return new SizeF(this.centerViewBounds.Size.Width - offset, this.centerViewBounds.Size.Height);
        }

        private void setSlidingFrameForOffset(float offset) 
        {
            this.offset = this.limitOffset(offset);
            this.SlidingControllerView.Frame = this.slidingRectForOffset(offset);

//delegate            this.performOffsetDelegate(@selector(viewDeckController:slideOffsetChanged:), this.offset);
        }

        private void hideAppropriateSideViews() 
        {
            this.LeftController.View.Hidden = this.SlidingControllerView.Frame.GetMinX() <= 0;

            this.RightController.View.Hidden = this.SlidingControllerView.Frame.GetMaxX() >= this.referenceBounds.Size.Width;
        }

        private static bool II_FLOAT_EQUAL(float a, float b)
        {
            return (a - b == 0);
        }
        
        private static float SLIDE_DURATION(bool animated, float duration)
        {
            return animated ? duration : 0;
        }

        private static float CLOSE_SLIDE_DURATION(bool animated)
        {
            return SLIDE_DURATION(animated, 0.3f);
        }

        private static float OPEN_SLIDE_DURATION(bool animated)
        {
            return SLIDE_DURATION(animated, 0.3f);
        }




        private void setMaxLedge(float maxLedge) 
        {
            this.maxLedge = maxLedge;

            if (this.LeftController != null && this.RightController != null) 
            {
                Console.WriteLine("ViewDeckController: warning: setting maxLedge with 2 side controllers. Value will be ignored.");
                return;
            }
            
            if (this.LeftController != null && this.LeftLedge > this.maxLedge) 
            {
                this.LeftLedge = this.maxLedge;
            }
            else if (this.RightController != null && this.RightLedge > this.maxLedge) 
            {
                this.RightLedge = this.maxLedge;
            }
            
            this.setSlidingFrameForOffset(this.offset);
        }


        public override void LoadView()
        {
            this.offset = 0;
            this.viewAppeared = false;

            this.View = new UIView();
            this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            this.View.AutosizesSubviews = true;
            this.View.ClipsToBounds = true;
        }

        public override void ViewDidLoad() 
        {
            base.ViewDidLoad();
            
            this.centerView = new UIView();
            this.centerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            this.centerView.AutosizesSubviews = true;
            this.centerView.ClipsToBounds = true;
            this.View.AddSubview(this.centerView);
            
            this.originalShadowRadius = 0;
            this.originalShadowOpacity = 0;
            this.originalShadowColor = null;
            this.originalShadowOffset = SizeF.Empty;
            this.originalShadowPath = null;
        }

        public override void ViewDidUnload()
        {
            this.CleanUp();
            base.ViewDidUnload();
        }



        public override void ViewWillAppear(bool animated) 
        {
            base.ViewWillAppear(animated);
            
            bool wasntAppeared = !this.viewAppeared;

            // was .Setting
            this.View.AddObserver(this, new NSString("bounds"),  NSKeyValueObservingOptions.Initial, IntPtr.Zero);

            NSAction applyViews = () => 
            {        
                this.CenterController.View.RemoveFromSuperview();
                this.centerView.AddSubview(this.CenterController.View);
                this.LeftController.View.RemoveFromSuperview();
                this.referenceView.InsertSubviewBelow(this.LeftController.View, this.SlidingControllerView);

                this.RightController.View.RemoveFromSuperview();
                this.referenceView.InsertSubviewBelow(this.RightController.View, this.SlidingControllerView);
                
                this.reapplySideController(this.LeftController);
                this.reapplySideController(this.RightController);
                
                this.setSlidingFrameForOffset(this.offset);
                this.SlidingControllerView.Hidden = false;
                
                this.centerView.Frame = this.centerViewBounds;
                this.CenterController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                this.CenterController.View.Frame = this.centerView.Bounds;
                
                this.LeftController.View.Frame = this.sideViewBounds;
                this.LeftController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                
                this.RightController.View.Frame = this.sideViewBounds;
                this.RightController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

                this.applyShadowToSlidingView();
            };

            if (this.setSlidingAndReferenceViews()) 
            {
                applyViews();
            }

            this.viewAppeared = true;

            // after 0.01 sec, since in certain cases the sliding view is reset.
//            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, 0.001 * NSEC_PER_SEC), dispatch_get_main_queue(), () =>
//            {
//                if (!this.referenceView) 
//                {
//                    this.setSlidingAndReferenceViews();
//                    applyViews();
//                }
//
//                this.setSlidingFrameForOffset(this.offset);
//                this.hideAppropriateSideViews();
//            });
            
            this.addPanners();
            
            if (this.SlidingControllerView.Frame.Location.X == 0.0f) 
            {
                this.centerViewVisible();
            }
            else
            {
                this.centerViewHidden();
            }

 //           this.relayAppearanceMethod:^(UIViewController *controller) 
 //               {
 //               [controller viewWillAppear:animated);
 //           } forced:wasntAppeared);
        }

        public override void ViewDidAppear(bool animated) 
        {
            base.ViewDidAppear(animated);
            
 //           this.relayAppearanceMethod:^(UIViewController *controller) {
 //               [controller viewDidAppear:animated);
 //           });
        }

        public override void ViewWillDisappear(bool animated) 
        {
            base.ViewWillDisappear(animated);
            
 //           this.relayAppearanceMethod:^(UIViewController *controller) 
 //                               {
 //               [controller viewWillDisappear:animated);
 //           });
            
            this.removePanners();
        }

        public override void ViewDidDisappear(bool animated) 
        {
            base.ViewDidDisappear(animated);
            
            try 
            {
                this.View.RemoveObserver(this, new NSString("bounds"));
            }
            catch(Exception ex)
            {
                //do nothing, obviously it wasn't attached because an exception was thrown
            }
            
//            this.relayAppearanceMethod:^(UIViewController *controller) {
//                [controller viewDidDisappear:animated);
//            });
        }




        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
            this.preRotationWidth = this.referenceBounds.Size.Width;
            this.preRotationCenterWidth = this.centerViewBounds.Size.Width;//todo: was - this.centerView.Bounds.Size.Width;
            
            if (this.RotationBehavior == ViewDeckRotationBehavior.KeepsViewSizes) 
           {
                this.leftWidth = this.LeftController.View.Frame.Size.Width;
                this.rightWidth = this.RightController.View.Frame.Size.Width;
            }
            
            bool should = true;
            if (this.CenterController != null)
                {
                should = this.CenterController.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation);
          }

            return should;
        }

         public override void WillAnimateRotation(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            base.WillAnimateRotation(toInterfaceOrientation, duration);
            
//            this.relayAppearanceMethod:^(UIViewController *controller) {
//                [controller willAnimateRotationToInterfaceOrientation:toInterfaceOrientation duration:duration);
//            });
            
            this.arrangeViewsAfterRotation();
        }

         public override void WillRotate(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            base.WillRotate(toInterfaceOrientation, duration);
            this.restoreShadowToSlidingView();
            
//            this.relayAppearanceMethod:^(UIViewController *controller) {
//                [controller willRotateToInterfaceOrientation:toInterfaceOrientation duration:duration);
//            });
        }

         public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
        {
            base.DidRotate(fromInterfaceOrientation);
            this.applyShadowToSlidingView();
            
//            this.relayAppearanceMethod:^(UIViewController *controller) {
//                [controller didRotateFromInterfaceOrientation:fromInterfaceOrientation);
//            });
        }

        private void arrangeViewsAfterRotation() 
        {
            if (this.preRotationWidth <= 0) return;
            
            float offset = this.SlidingControllerView.Frame.Location.X;

            if (this.resizesCenterView != null && offset == 0) 
            {
                offset = offset + (this.preRotationCenterWidth - this.preRotationWidth);
            }
            
            if (this.RotationBehavior == ViewDeckRotationBehavior.KeepsLedgeSizes) 
            {
                if (offset > 0) 
                {
                    offset = this.referenceBounds.Size.Width - this.preRotationWidth + offset;
                }
                else if (offset < 0) 
                {
                    offset = offset + this.preRotationWidth - this.referenceBounds.Size.Width;
                }
            }
            else 
            {
                this.LeftLedge = this.LeftLedge + this.referenceBounds.Size.Width - this.preRotationWidth; 
                this.RightLedge = this.RightLedge + this.referenceBounds.Size.Width - this.preRotationWidth; 
                this.maxLedge = this.maxLedge + this.referenceBounds.Size.Width - this.preRotationWidth; 
            }

            this.setSlidingFrameForOffset(offset);
            
            this.preRotationWidth = 0;
        }


        private void showCenterView() 
        {
            this.showCenterView(true);
        }

        private void showCenterView(bool animated) 
        {
            this.showCenterView(animated, null);
        }

        private void showCenterView(bool animated, Action<ViewDeckController> completed)
        {
            bool mustRunCompletion = completed != null;

            if (this.LeftController != null&& !this.LeftController.View.Hidden) 
            {
                this.closeLeftViewAnimated(animated, completed);
                mustRunCompletion = false;
            }
            
            if (this.RightController != null && !this.RightController.View.Hidden) 
            {
                this.closeRightViewAnimated(animated, completed);
                mustRunCompletion = false;
            }
            
            if (mustRunCompletion)
                completed(this);
        }

        private bool toggleLeftView() 
        {
            return this.toggleLeftViewAnimated(true);
        }

        private bool openLeftView() 
        {
            return this.openLeftViewAnimated(true);
        }

        private bool closeLeftView()
        {
            return this.closeLeftViewAnimated(true);
        }

        private bool toggleLeftViewAnimated(bool animated)
        {
            return this.toggleLeftViewAnimated(animated, null);
        }

        private bool toggleLeftViewAnimated(bool animated, Action<ViewDeckController> completed)
        {
            if (this.LeftControllerIsClosed) 
            {
                return this.openLeftViewAnimated(animated, completed);
            }
            else
            {
                return this.closeLeftViewAnimated(animated, completed);
            }
        }

        private bool openLeftViewAnimated(bool animated) 
        {
            return this.openLeftViewAnimated(animated, null);
        }

        private bool openLeftViewAnimated(bool animated, Action<ViewDeckController> completed)
        {
            return this.openLeftViewAnimated(animated, UIViewAnimationOptions.CurveEaseInOut, true, completed);
        }

        private bool openLeftViewAnimated(bool animated, bool callDelegate, Action<ViewDeckController> completed)
        {
            return this.openLeftViewAnimated(animated, UIViewAnimationOptions.CurveEaseInOut, callDelegate, completed);
        }

        private bool openLeftViewAnimated(bool animated, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed)
        {
            if (this.LeftController == null || II_FLOAT_EQUAL(this.SlidingControllerView.Frame.GetMinX(), this.LeftLedge)) return true;


            // check the delegate to allow opening
//delegate            if (callDelegate && !this.checkDelegate:@selector(viewDeckControllerWillOpenLeftView:animated:) animated:animated]) return false;

            // also close the right view if it's open. Since the delegate can cancel the close, check the result.
//delegate            if (callDelegate && !this.closeRightViewAnimated(animated, options, callDelegate, completed]) return false;
            
            UIView.Animate(OPEN_SLIDE_DURATION(animated), 0, options | UIViewAnimationOptions.LayoutSubviews | UIViewAnimationOptions.BeginFromCurrentState, () =>
                           {
                this.LeftController.View.Hidden = false;
                this.setSlidingFrameForOffset(this.referenceBounds.Size.Width - this.LeftLedge);
                this.centerViewHidden();
            }, () =>
            {
                if (completed != null) completed(this);
                if (callDelegate) 
                {
//delegate                this.performDelegate:@selector(viewDeckControllerDidOpenLeftView:animated:) animated:animated);
                }

            });
            
            return true;
        }

        private bool openLeftViewBouncing(Action<ViewDeckController> bounced)
        {
            return this.openLeftViewBouncing(bounced, null);
        }

        private bool openLeftViewBouncing(Action<ViewDeckController> bounced, Action<ViewDeckController>completed) 
        {
            return this.openLeftViewBouncing(bounced, true, completed);
        }

        private bool openLeftViewBouncing(Action<ViewDeckController> bounced, bool callDelegate, Action<ViewDeckController> completed) 
        {
            return this.openLeftViewBouncing(bounced, UIViewAnimationOptions.CurveEaseInOut, true, completed);
        }

        private bool openLeftViewBouncing(Action<ViewDeckController> bounced, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed)
        {
            if (this.LeftController == null || II_FLOAT_EQUAL(this.SlidingControllerView.Frame.GetMinX(), this.LeftLedge)) return true;
            
            // check the delegate to allow opening
//            if (callDelegate && !this.checkDelegate:@selector(viewDeckControllerWillOpenLeftView:animated:) animated:YES]) return false;

            // also close the right view if it's open. Since the delegate can cancel the close, check the result.
//            if (callDelegate && !this.closeRightViewAnimated:YES options:options callDelegate:callDelegate completion:completed]) return false;
            
            // first open the view completely, run the block (to allow changes)
            UIView.Animate(OPEN_SLIDE_DURATION(true), 0, UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.LayoutSubviews, () =>
            {
                this.LeftController.View.Hidden = false;
                this.setSlidingFrameForOffset(this.referenceBounds.Size.Width);
            }, () => {
                // run block if it's defined
                if (bounced != null) bounced(this);
                this.centerViewHidden();
                
                // now slide the view back to the ledge position
                UIView.Animate(OPEN_SLIDE_DURATION(true), 0, options | UIViewAnimationOptions.LayoutSubviews | UIViewAnimationOptions.BeginFromCurrentState,
                               () => {
                    this.setSlidingFrameForOffset(this.referenceBounds.Size.Width - this.LeftLedge);
                }, () => {
                    if (completed != null) completed(this);
//                    if (callDelegate) this.performDelegate:@selector(viewDeckControllerDidOpenLeftView:animated:) animated:YES);
                });
            });
            
            return true;
        }

        private bool closeLeftViewAnimated(bool animated) 
        {
            return this.closeLeftViewAnimated(animated, null);
        }

        private bool closeLeftViewAnimated(bool animated, Action<ViewDeckController> completed)
        {
            return this.closeLeftViewAnimated(animated,true, completed);
        }

        private bool closeLeftViewAnimated(bool animated, bool callDelegate, Action<ViewDeckController> completed) 
        {
            return this.closeLeftViewAnimated(animated, UIViewAnimationOptions.CurveEaseInOut, callDelegate, completed);
        }

        private bool closeLeftViewAnimated(bool animated, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed) 
        {
            if (this.LeftControllerIsClosed) return true;
            
            // check the delegate to allow closing
//            if (callDelegate && !this.checkDelegate:@selector(viewDeckControllerWillCloseLeftView:animated:) animated:animated]) return NO;
            
            UIView.Animate(CLOSE_SLIDE_DURATION(animated), 0, options | UIViewAnimationOptions.LayoutSubviews, () => {
                this.setSlidingFrameForOffset(0);
                this.centerViewVisible();
            }, () =>  {
                this.hideAppropriateSideViews();
                if (completed != null) completed(this);
                if (callDelegate) 
                {
//                    this.performDelegate:@selector(viewDeckControllerDidCloseLeftView:animated:) animated:animated);
//                    this.performDelegate:@selector(viewDeckControllerDidShowCenterView:animated:) animated:animated);
                }
            });
            
            return true;
        }

        private bool closeLeftViewBouncing(Action<ViewDeckController> bounced) 
        {
            return this.closeLeftViewBouncing(bounced, null);
        }

        private bool closeLeftViewBouncing(Action<ViewDeckController> bounced, Action<ViewDeckController> completed) 
        {
            return this.closeLeftViewBouncing(bounced, true, completed);
        }

        private bool closeLeftViewBouncing(Action<ViewDeckController> bounced, bool callDelegate, Action<ViewDeckController> completed) 
        {
            if (this.LeftControllerIsClosed) return true;
            
            // check the delegate to allow closing
//            if (callDelegate && !this.checkDelegate:@selector(viewDeckControllerWillCloseLeftView:animated:) animated:YES]) return NO;
            
            // first open the view completely, run the block (to allow changes) and close it again.
            UIView.Animate(OPEN_SLIDE_DURATION(true), 0, UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.LayoutSubviews,
                           () => 
                           {
                this.setSlidingFrameForOffset(this.referenceBounds.Size.Width);
            }, () => 
            {
                // run block if it's defined
                if (bounced != null) bounced(this);

//                if (callDelegate && this.delegate && [this.delegate respondsToSelector:@selector(viewDeckController:didBounceWithClosingController:)]) 
//                    [this.delegate viewDeckController:self didBounceWithClosingController:this.leftController);
                
                UIView.Animate(CLOSE_SLIDE_DURATION(true), 0, UIViewAnimationOptions.CurveEaseOut | UIViewAnimationOptions.LayoutSubviews, () => {
                    this.setSlidingFrameForOffset(0);
                    this.centerViewVisible();
                } , () => {
                    this.hideAppropriateSideViews();
                    if (completed != null) completed(this);
                    if (callDelegate) 
                    {
//                        this.performDelegate:@selector(viewDeckControllerDidCloseLeftView:animated:) animated:YES);
//                        this.performDelegate:@selector(viewDeckControllerDidShowCenterView:animated:) animated:YES);
                    }
                });
            });
            
            return true;
        }


        private bool toggleRightView() 
        {
            return this.toggleRightViewAnimated(true);
        }

        private bool openRightView() 
        {
            return this.openRightViewAnimated(true);
        }

        private bool closeRightView() 
        {
            return this.closeRightViewAnimated(true);
        }

        private bool toggleRightViewAnimated(bool animated)
        {
            return this.toggleRightViewAnimated(animated, null);
        }

        private bool toggleRightViewAnimated(bool animated, Action<ViewDeckController> completed) 
        {
            if (this.RightControllerIsClosed) 
                {
                return this.openRightViewAnimated(animated, completed);
                }
                else
                {
                    return this.closeRightViewAnimated(animated, completed);
                }
        }

        private bool openRightViewAnimated(bool animated)
        {
            return this.openRightViewAnimated(animated, null);
        }

        private bool openRightViewAnimated(bool animated, Action<ViewDeckController> completed) 
        {
            return this.openRightViewAnimated(animated, UIViewAnimationOptions.CurveEaseInOut,true, completed);
        }

        private bool openRightViewAnimated(bool animated, bool callDelegate, Action<ViewDeckController> completed)
        {
            return this.openRightViewAnimated(animated, UIViewAnimationOptions.CurveEaseInOut, callDelegate, completed);
        }

        private bool openRightViewAnimated(bool animated, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed)
        {
            if (this.RightController == null || II_FLOAT_EQUAL(this.SlidingControllerView.Frame.GetMaxX(), this.RightLedge)) return true;
            
            // check the delegate to allow opening
//            if (callDelegate && !this.checkDelegate:@selector(viewDeckControllerWillOpenRightView:animated:) animated:animated]) return NO;

            // also close the left view if it's open. Since the delegate can cancel the close, check the result.
//            if (callDelegate && !this.closeLeftViewAnimated:animated options:options callDelegate:callDelegate completion:completed]) return NO;
            
            UIView.Animate(OPEN_SLIDE_DURATION(animated), 0, options | UIViewAnimationOptions.LayoutSubviews, () => {
                this.RightController.View.Hidden = false;
                this.setSlidingFrameForOffset(this.RightLedge - this.referenceBounds.Size.Width);
                this.centerViewHidden();
            }, () => {
                if (completed != null) completed(this);
//                if (callDelegate) this.performDelegate:@selector(viewDeckControllerDidOpenRightView:animated:) animated:animated);
            });

            return true;
        }

        private bool openRightViewBouncing(Action<ViewDeckController> bounced) 
        {
            return this.openRightViewBouncing(bounced, null);
        }

        private bool openRightViewBouncing(Action<ViewDeckController> bounced, Action<ViewDeckController> completed) 
        {
            return this.openRightViewBouncing(bounced, true, completed);
        }

        private bool openRightViewBouncing(Action<ViewDeckController> bounced, bool callDelegate, Action<ViewDeckController> completed)
        {
            return this.openRightViewBouncing(bounced, UIViewAnimationOptions.CurveEaseInOut, true, completed);
        }

        private bool openRightViewBouncing(Action<ViewDeckController> bounced, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed)
        {
            if (this.RightController == null || II_FLOAT_EQUAL(this.SlidingControllerView.Frame.GetMinX(), this.RightLedge)) return true;
            
            // check the delegate to allow opening
//            if (callDelegate && !this.checkDelegate:@selector(viewDeckControllerWillOpenRightView:animated:) animated:YES]) return NO;

            // also close the right view if it's open. Since the delegate can cancel the close, check the result.
//            if (callDelegate && !this.closeLeftViewAnimated:YES options:options callDelegate:callDelegate completion:completed]) return NO;
            
            // first open the view completely, run the block (to allow changes)
            UIView.Animate(OPEN_SLIDE_DURATION(true), 0, UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.LayoutSubviews, () => {
                this.RightController.View.Hidden = false;
                this.setSlidingFrameForOffset(-this.referenceBounds.Size.Width);
            }, () =>  {
                // run block if it's defined
                if (bounced != null) bounced(this);
                this.centerViewHidden();
                
                // now slide the view back to the ledge position
                UIView.Animate(OPEN_SLIDE_DURATION(true), 0, options | UIViewAnimationOptions.LayoutSubviews | UIViewAnimationOptions.BeginFromCurrentState, () => {
                    this.setSlidingFrameForOffset(this.RightLedge - this.referenceBounds.Size.Width);
                }, () => {
                    if (completed != null) completed(this);
//                    if (callDelegate) this.performDelegate:@selector(viewDeckControllerDidOpenRightView:animated:) animated:YES);
                });
            });
            
            return true;
        }

        private bool closeRightViewAnimated(bool animated)
        {
            return this.closeRightViewAnimated(animated, null);
        }

        private bool closeRightViewAnimated(bool animated, Action<ViewDeckController> completed)
        {
            return this.closeRightViewAnimated(animated, UIViewAnimationOptions.CurveEaseInOut, true, completed);
        }

        private bool closeRightViewAnimated(bool animated, bool callDelegate, Action<ViewDeckController> completed)
        {
            return this.openRightViewAnimated(animated, UIViewAnimationOptions.CurveEaseInOut, callDelegate, completed);
        }

        private bool closeRightViewAnimated(bool animated, UIViewAnimationOptions options, bool callDelegate, Action<ViewDeckController> completed) 
        {
            if (this.RightControllerIsClosed) return true;
            
            // check the delegate to allow closing
//            if (callDelegate && !this.checkDelegate:@selector(viewDeckControllerWillCloseRightView:animated:) animated:animated]) return NO;
            
            UIView.Animate(CLOSE_SLIDE_DURATION(animated), 0, options | UIViewAnimationOptions.LayoutSubviews, () => {
                this.setSlidingFrameForOffset(0);
                this.centerViewVisible();
            }, () => {
                if (completed != null) completed(this);
                this.hideAppropriateSideViews();
                if (callDelegate) {
//                    this.performDelegate:@selector(viewDeckControllerDidCloseRightView:animated:) animated:animated);
//                    this.performDelegate:@selector(viewDeckControllerDidShowCenterView:animated:) animated:animated);
                }
            });
            
            return true;
        }

        private bool closeRightViewBouncing(Action<ViewDeckController> bounced) 
        {
            return this.closeRightViewBouncing(bounced, null);
        }

        private bool closeRightViewBouncing(Action<ViewDeckController> bounced, Action<ViewDeckController> completed) 
        {
            return this.closeRightViewBouncing(bounced, true, completed);
        }

        private bool closeRightViewBouncing(Action<ViewDeckController> bounced, bool callDelegate, Action<ViewDeckController> completed) 
        {
            if (this.RightControllerIsClosed) return true;
            
            // check the delegate to allow closing
//            if (callDelegate && !this.checkDelegate:@selector(viewDeckControllerWillCloseRightView:animated:) animated:YES]) return NO;
            
            UIView.Animate(OPEN_SLIDE_DURATION(true), 0,  UIViewAnimationOptions.CurveEaseIn | UIViewAnimationOptions.LayoutSubviews, () => {
                this.setSlidingFrameForOffset(-this.referenceBounds.Size.Width);
            }, () =>  {
                if (bounced != null) bounced(this);
//                if (callDelegate && this.delegate && [this.delegate respondsToSelector:@selector(viewDeckController:didBounceWithClosingController:)]) 
//                    [this.delegate viewDeckController:self didBounceWithClosingController:this.rightController);
                
                UIView.Animate(CLOSE_SLIDE_DURATION(true), 0, UIViewAnimationOptions.CurveEaseOut | UIViewAnimationOptions.LayoutSubviews, () => {
                    this.setSlidingFrameForOffset(0);
                    this.centerViewVisible();
                }, () =>  {
                    this.hideAppropriateSideViews();
                    if (completed != null) completed(this);
//                    this.performDelegate:@selector(viewDeckControllerDidCloseRightView:animated:) animated:YES);
//                    this.performDelegate:@selector(viewDeckControllerDidShowCenterView:animated:) animated:YES);
                });
            });
            
            return true;
        }

        private static RectangleF  RectangleFOffset(RectangleF rect, float dx, float dy)
        {
            // todo: is this correct
            return rect.Inset(dx, dy);
        }

        private void rightViewPushViewControllerOverCenterController(UIViewController controller) 
        {
            Debug.Assert(this.CenterController.GetType().IsSubclassOf(typeof(UINavigationController)), "cannot rightViewPushViewControllerOverCenterView when center controller is not a navigation controller");

            UIGraphics.BeginImageContextWithOptions(this.View.Bounds.Size, true, 0);

            CGContext context = UIGraphics.GetCurrentContext();
            this.View.Layer.RenderInContext(context);

            UIImage deckshot = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            
            UIImageView shotView = new UIImageView(deckshot);
            shotView.Frame = this.View.Frame; 
            this.View.Superview.AddSubview(shotView);

            RectangleF targetFrame = this.View.Frame; 

            this.View.Frame = RectangleFOffset(this.View.Frame, this.View.Frame.Size.Width, 0);
            
            this.closeRightViewAnimated(true);

            UINavigationController navController = ((UINavigationController)this.CenterController);
            navController.PushViewController(controller, false);
            
            UIView.Animate(0.3, 0, UIViewAnimationOptions.TransitionNone, () =>
           {
                shotView.Frame = RectangleFOffset(shotView.Frame, -this.View.Frame.Size.Width, 0);
                this.View.Frame = targetFrame;
            },
                () => 
                {
                shotView.RemoveFromSuperview();
            });
        }



       // #pragma mark - Pre iOS5 message relaying

        private void relayAppearanceMethod(Action<UIViewController> relay, bool forced) 
        {
//            bool shouldRelay = ![self respondsToSelector:@selector(automaticallyForwardAppearanceAndRotationMethodsToChildViewControllers)] || ![self performSelector:@selector(automaticallyForwardAppearanceAndRotationMethodsToChildViewControllers)];
//            
//            // don't relay if the controller supports automatic relaying
//            if (!shouldRelay && !forced) 
//                return;                                                                                                                                       
//            
//            relay(self.centerController);
//            relay(self.leftController);
//            relay(self.rightController);
        }

        private void relayAppearanceMethod(Action<UIViewController> relay)
        {
//            [self relayAppearanceMethod:relay forced:NO];
        }

        //#pragma mark - center view hidden stuff

        private void centerViewVisible()
        {
            this.removePanners();
            if (this.centerTapper != null) 
            {
// todo:                this.centerTapper.RemoveTarget(this, @selector(centerTapped), UIControlEventTouchUpInside);
                this.centerTapper.RemoveFromSuperview();
            }

            this.centerTapper = null;
            this.addPanners();
        }

        private void centerViewHidden() 
        {
            if (this.centerHiddenInteractivity == CenterHiddenInteractivity.UserInteractive) 
                return;
            
            this.removePanners();

            if (this.centerTapper == null) 
            {
                this.centerTapper =  new UIButton(UIButtonType.Custom);
                this.centerTapper.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                this.centerTapper.Frame = this.centerView.Bounds;
                this.centerView.AddSubview(this.centerTapper);
                this.centerTapper.AddTarget(this, new Selector("centerTapped"), UIControlEvent.TouchUpInside);
                this.centerTapper.BackgroundColor = UIColor.Clear;
                
            }

            this.centerTapper.Frame = this.centerView.Bounds;
            this.addPanners();
        }

        [Export("centerTapped")]
        private void centerTapped() 
        {
            // todo: handle additinal cases better
            if (this.centerHiddenInteractivity != CenterHiddenInteractivity.UserInteractive) 
            {
                if (this.LeftController != null && this.SlidingControllerView.Frame.GetMinX() > 0) 
                {
                    if (this.centerHiddenInteractivity == CenterHiddenInteractivity.NotUserInteractiveWithTapToClose) 
                    {
                        this.closeLeftView();
                    }
                    else
                    {
                        this.closeLeftViewBouncing(null);
                    }
                }

                if (this.RightController != null && this.SlidingControllerView.Frame.GetMinX() < 0) 
                {
                    if (this.centerHiddenInteractivity == CenterHiddenInteractivity.NotUserInteractiveWithTapToClose) 
                    {
                        this.closeRightView();
                    }
                    else
                    {
                        this.closeRightViewBouncing(null);
                    }
                }
                
            }
        }

        //#pragma mark - Panning

        [Export("gestureRecognizerShouldBegin:")]
        private bool gestureRecognizerShouldBegin(UIGestureRecognizer gestureRecognizer)
        {
            float px = this.SlidingControllerView.Frame.Location.X;
            if (px != 0) return true;
                
            float x = this.locationOfPanner((UIPanGestureRecognizer)gestureRecognizer);
            bool ok =  true;

            if (x > 0) 
            {
// todo                ok = [self checkDelegate:@selector(viewDeckControllerWillOpenLeftView:animated:) animated:NO];
                if (!ok)
                    this.closeLeftViewAnimated(false);
            }
            else if (x < 0) 
            {
// todo                ok = [self checkDelegate:@selector(viewDeckControllerWillOpenRightView:animated:) animated:NO];
                if (!ok)
                    this.closeRightViewAnimated(false);
            }
            
            return ok;
        }

        [Export("gestureRecognizer:shouldReceiveTouch:")]
        private bool gestureRecognizer(UIGestureRecognizer gestureRecognizer, UITouch touch) 
        {
            this.panOrigin = this.SlidingControllerView.Frame.Location.X;
            return true;
        }

        private float locationOfPanner(UIPanGestureRecognizer panner) 
        {
            PointF pan = panner.TranslationInView(this.referenceView);
            float x = pan.X + this.panOrigin;
            
            if (this.LeftController == null) x = Math.Min(0, x);

            if (this.RightController == null) x = Math.Max(0, x);
            
            float w = this.referenceBounds.Size.Width;
            float lx = Math.Max(Math.Min(x, w - this.LeftLedge), -w + this.RightLedge);
            
            if (this.Elastic) 
            {
                float dx = Math.Abs(x) - Math.Abs(lx);

                if (dx > 0) 
                {
                    dx = dx / (float)Math.Log(dx + 1) * 2;
                    x = lx + (x < 0 ? -dx : dx);
                }
            }
            else 
            {
                x = lx;
            }
            
            return this.limitOffset(x);
        }

        [Export("panned:")]
        private void panned(UIPanGestureRecognizer panner) 
        {
            if (!this.Enabled) return;

            float px = this.SlidingControllerView.Frame.Location.X;
            float x = this.locationOfPanner(panner);
            float w = this.referenceBounds.Size.Width;

            Selector didCloseSelector = null;
            Selector didOpenSelector = null;
            
            // if we move over a boundary while dragging, ... 
            if (px <= 0 && x >= 0 && px != x) 
            {
                // ... then we need to check if the other side can open.
                if (px < 0) 
                {
                    bool canClose = true;// todo: this.checkDelegate:@selector(viewDeckControllerWillCloseRightView:animated:) animated:NO];
                    if (!canClose)
                        return;
                    didCloseSelector = new Selector("viewDeckControllerDidCloseRightView:animated:");
                }

                if (x > 0) 
                {
                    bool canOpen = true;// todo: [self checkDelegate:@selector(viewDeckControllerWillOpenLeftView:animated:) animated:NO];
                    didOpenSelector = new Selector("viewDeckControllerDidOpenLeftView:animated:");
                    if (!canOpen) 
                    {
                        this.closeRightViewAnimated(false);
                        return;
                    }
                }
            }
            else if (px >= 0 && x <= 0 && px != x) 
            {
                if (px > 0) 
                {
                    bool canClose = true;// todo: [self checkDelegate:@selector(viewDeckControllerWillCloseLeftView:animated:) animated:NO];
                    if (!canClose) 
                    {
                        return;
                    }

                    didCloseSelector = new Selector("viewDeckControllerDidCloseLeftView:animated:");
                }

                if (x < 0) 
                {
                    bool canOpen = true;// todo: [self checkDelegate:@selector(viewDeckControllerWillOpenRightView:animated:) animated:NO];
                    didOpenSelector = new Selector("viewDeckControllerDidOpenRightView:animated:");
                    if (!canOpen) 
                    {
                        this.closeLeftViewAnimated(false);
                        return;
                    }
                }
            }
            
            this.setSlidingFrameForOffset(x);
            
            bool rightWasHidden = this.RightController.View.Hidden;
            bool leftWasHidden = this.LeftController.View.Hidden;
            
            // todo: [self performOffsetDelegate:@selector(viewDeckController:didPanToOffset:) offset:x];
            
            if (panner.State == UIGestureRecognizerState.Ended) 
            {
                if (this.SlidingControllerView.Frame.Location.X == 0.0f) 
                {
                    this.centerViewVisible();
                }
                else
                {
                    this.centerViewHidden();
                }

                float lw3 = (w - this.LeftLedge) / 3.0f;
                float rw3 = (w - this.RightLedge) / 3.0f;
                float velocity = panner.VelocityInView(this.referenceView).X;

                if (Math.Abs(velocity) < 500) 
                {
                    // small velocity, no movement
                    if (x >= w - this.LeftLedge - lw3) 
                    {
                        this.openLeftViewAnimated(true, UIViewAnimationOptions.CurveEaseOut, false, null);
                    }
                    else if (x <= this.RightLedge + rw3 - w) 
                    {
                        this.openRightViewAnimated(true, UIViewAnimationOptions.CurveEaseOut, false, null);
                    }
                    else
                    {
                        this.showCenterView(true);
                    }
                }
                else if (velocity < 0) 
                {
                    // swipe to the left
                    if (x < 0) 
                    {
                        this.openRightViewAnimated(true, UIViewAnimationOptions.CurveEaseOut, true, null);
                    }
                    else 
                    {
                        this.showCenterView(true);
                    }
                }
                else if (velocity > 0) 
                {
                    // swipe to the right
                    if (x > 0) 
                    {
                        this.openLeftViewAnimated(true, UIViewAnimationOptions.CurveEaseOut, true, null);
                    }
                    else 
                    {
                        this.showCenterView(true);
                    }
                }
            }
            else
            {
                this.hideAppropriateSideViews();
            }

            if (didCloseSelector != null)
            {
                // todo: [self performDelegate:didCloseSelector animated:NO];
            }

            if (didOpenSelector != null)
            {
                // todo: [self performDelegate:didOpenSelector animated:NO];
            }
        }


        private void addPanner(UIView view) 
        {
            if (view == null) return;

            UIPanGestureRecognizer panner = new UIPanGestureRecognizer(this, new Selector("panned:"));

            panner.CancelsTouchesInView = true;
            panner.WeakDelegate = this;

            this.View.AddGestureRecognizer(panner);
            this.panners.Add(panner);
        }


        private void addPanners() 
        {
            this.removePanners();
            
            switch (this.PanningMode) 
            {
                case ViewDeckPanningMode.NoPanning: 
                    break;
                    
                case ViewDeckPanningMode.FullViewPanning:
                    this.addPanner(this.SlidingControllerView);

                    // also add to disabled center
                    if (this.centerTapper != null)
                        this.addPanner(this.centerTapper);

                    // also add to navigationbar if present
                    if (this.NavigationController != null && !this.NavigationController.NavigationBarHidden) 
                        this.addPanner(this.NavigationController.NavigationBar);

                    break;
                    
                case ViewDeckPanningMode.NavigationBarPanning:
                    if (this.NavigationController != null && !this.NavigationController.NavigationBarHidden) 
                    {
                        this.addPanner(this.NavigationController.NavigationBar);
                    }
                    
                    if (this.CenterController.NavigationController != null && !this.CenterController.NavigationController.NavigationBarHidden) 
                    {
                        this.addPanner(this.CenterController.NavigationController.NavigationBar);
                    }
                    
                    if (this.CenterController.GetType().IsSubclassOf(typeof(UINavigationController)) && !((UINavigationController)this.CenterController).NavigationBarHidden) 
                    {
                        this.addPanner(((UINavigationController)this.CenterController).NavigationBar);
                    }

                    break;
                case ViewDeckPanningMode.PanningViewPanning:
                    if (this.PanningView != null) 
                    {
                        this.addPanner(this.PanningView);
                    }

                    break;
            }
        }


        private void removePanners() 
        {
            foreach (var panner in this.panners) 
            {
                panner.View.RemoveGestureRecognizer(panner);
            }

            this.panners.Clear();
        }

        //#pragma mark - Delegate convenience methods

//        private bool checkDelegate(SEL selector, bool animated) 
//        {
//            BOOL ok = YES;
//            // used typed message send to properly pass values
//            BOOL (*objc_msgSendTyped)(id self, SEL _cmd, IIViewDeckController* foo, BOOL animated) = (void*)objc_msgSend;
//            
//            if (self.delegate && [self.delegate respondsToSelector:selector]) 
//                ok = ok & objc_msgSendTyped(self.delegate, selector, self, animated);
//            
//            for (UIViewController* controller in self.controllers) {
//                // check controller first
//                if ([controller respondsToSelector:selector] && (id)controller != (id)self.delegate) 
//                    ok = ok & objc_msgSendTyped(controller, selector, self, animated);
//                // if that fails, check if it's a navigation controller and use the top controller
//                else if ([controller isKindOfClass:[UINavigationController class]]) {
//                    UIViewController* topController = ((UINavigationController*)controller).topViewController;
//                    if ([topController respondsToSelector:selector] && (id)topController != (id)self.delegate) 
//                        ok = ok & objc_msgSendTyped(topController, selector, self, animated);
//                }
//            }
//            
//            return ok;
//        }

//        private void performDelegate(SEL selector, bool animated) 
//        {
//            // used typed message send to properly pass values
//            void (*objc_msgSendTyped)(id self, SEL _cmd, IIViewDeckController* foo, BOOL animated) = (void*)objc_msgSend;
//
//            if (self.delegate && [self.delegate respondsToSelector:selector]) 
//                objc_msgSendTyped(self.delegate, selector, self, animated);
//            
//            for (UIViewController* controller in self.controllers) {
//                // check controller first
//                if ([controller respondsToSelector:selector] && (id)controller != (id)self.delegate) 
//                    objc_msgSendTyped(controller, selector, self, animated);
//                // if that fails, check if it's a navigation controller and use the top controller
//                else if ([controller isKindOfClass:[UINavigationController class]]) {
//                    UIViewController* topController = ((UINavigationController*)controller).topViewController;
//                    if ([topController respondsToSelector:selector] && (id)topController != (id)self.delegate) 
//                        objc_msgSendTyped(topController, selector, self, animated);
//                }
//            }
//        }

//        private void performOffsetDelegate(SEL selector, float offset) 
//        {
//            void (*objc_msgSendTyped)(id self, SEL _cmd, IIViewDeckController* foo, CGFloat offset) = (void*)objc_msgSend;
//            if (self.delegate && [self.delegate respondsToSelector:selector]) 
//                objc_msgSendTyped(self.delegate, selector, self, offset);
//            
//            for (UIViewController* controller in self.controllers) {
//                // check controller first
//                if ([controller respondsToSelector:selector] && (id)controller != (id)self.delegate) 
//                    objc_msgSendTyped(controller, selector, self, offset);
//                
//                // if that fails, check if it's a navigation controller and use the top controller
//                else if ([controller isKindOfClass:[UINavigationController class]]) {
//                    UIViewController* topController = ((UINavigationController*)controller).topViewController;
//                    if ([topController respondsToSelector:selector] && (id)topController != (id)self.delegate) 
//                        objc_msgSendTyped(topController, selector, self, offset);
//                }
//            }
//        }


       // #pragma mark - Properties


        // todo: is controllerStore a ref parametsr
        private void applySideController(ref UIViewController controllerStore, UIViewController newController, UIViewController otherController, 
                                             NSAction clearOtherController) 
        {
            //void(^beforeBlock)(UIViewController* controller) = ^(UIViewController* controller){};
            //void(^afterBlock)(UIViewController* controller, BOOL left) = ^(UIViewController* controller, BOOL left){};

            Action<UIViewController> beforeBlock = (x) => {};
            Action<UIViewController, bool> afterBlock = (x, y) => {};

            if (this.viewAppeared) 
            {
                beforeBlock = (controller) => 
                {
//                    controller.vdc_viewWillDisappear(false);
                    controller.View.RemoveFromSuperview();
//                    controller.vdc_viewDidDisappear(false);
                };

                afterBlock = (controller, left) => 
                {
//                    controller.vdc_viewWillAppear(false);
                    controller.View.Hidden = left ? this.SlidingControllerView.Frame.Location.X <= 0 : this.SlidingControllerView.Frame.Location.X >= 0;
                    controller.View.Frame = this.referenceBounds;
                    controller.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                    if (this.slidingController != null)
                    {
                        this.referenceView.InsertSubviewBelow(controller.View, this.SlidingControllerView);
                    }
                    else
                    {
                        this.referenceView.AddSubview(controller.View);
                    }

//                    controller.vdc_viewDidAppear(false);
                };
            }
            
            // start the transition
            if (controllerStore != null) 
            {
                controllerStore.WillMoveToParentViewController(null);
                if (newController == this.CenterController) 
                {
                    this.CenterController = null;
                }

                if (newController == otherController && clearOtherController != null) clearOtherController();

                beforeBlock(controllerStore);

//                controllerStore.setViewDeckController(null);
                controllerStore.RemoveFromParentViewController();
                controllerStore.DidMoveToParentViewController(null);
            }
            
            // make the switch
            if (controllerStore != newController) 
            {
                // todo: dispose II_RELEASE(*controllerStore);
                controllerStore = newController;
                //II_RETAIN(*controllerStore);
            }
            
            if (controllerStore != null) 
            {
                newController.WillMoveToParentViewController(null);
                newController.RemoveFromParentViewController();
                newController.DidMoveToParentViewController(null);
                
                // and finish the transition
                UIViewController parentController = (this.referenceView == this.View) ? this : this.GetGrandParent();
                if (parentController != null)
                {
                    parentController.AddChildViewController(controllerStore);
                }

//                controllerStore.setViewDeckController(this);

                afterBlock(controllerStore, controllerStore == this.LeftController);

                controllerStore.DidMoveToParentViewController(parentController);
            }
        }

        private UIViewController GetGrandParent()
        {
            if (this.ParentViewController != null)
            {
                return this.ParentViewController.ParentViewController;
            }

            return null;
        }

        private void reapplySideController(UIViewController controllerStore) 
        {
            this.applySideController(ref controllerStore, controllerStore, null, null);
        }


        #region Property Setters

        /// <summary>
        /// Sets the left controller, clears the right controller if viewController is already the right controller
        /// </summary>
        private void SetLeftController(UIViewController viewController) 
        {
            if (this.LeftController == viewController) 
            {
                return;
            }

            this.applySideController(ref this._leftController, viewController, this.RightController, () => { this.RightController = null; });
        }

        /// <summary>
        /// Sets the right controller, clears the left controller if viewController is already the left controller
        /// </summary>
        private void SetRightController(UIViewController viewController)
        {
            if (this.RightController == viewController) 
            {
                return;
            }

            this.applySideController(ref this._rightController, viewController, this.LeftController, () => { this.LeftController = null; });
        }

        /// <summary>
        /// Set the center controller
        /// </summary>
        private void SetCenterController(UIViewController centerController) 
        {
            if (this.CenterController == centerController) 
            {
                return;
            }

            Action<UIViewController> beforeBlock = (x) => {};
            Action<UIViewController> afterBlock = (x) => {};

            var currentFrame = this.referenceBounds;

            if (this.viewAppeared) 
            {
                beforeBlock = (controller) => 
                {
// todo:                    controller.vdc_viewWillDisappear(false);
                    this.restoreShadowToSlidingView();
                    this.removePanners();
                    controller.View.RemoveFromSuperview();
// todo:                    controller.vdc_viewDidDisappear(false);
                    this.centerView.RemoveFromSuperview();
                };

                afterBlock = (controller) => 
                {
                    this.View.AddSubview(this.centerView);
// todo:                    controller.vdc_viewWillAppear(false);

                    UINavigationController navController = centerController.GetType().IsSubclassOf(typeof(UINavigationController)) 
                    ? (UINavigationController)centerController 
                    : null;

                    bool barHidden = false;
                    if (navController != null && !navController.NavigationBarHidden) 
                    {
                        barHidden = true;
                        navController.NavigationBarHidden = true;
                    }
                    
                    this.setSlidingAndReferenceViews();
                    controller.View.Frame = currentFrame;
                    controller.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                    controller.View.Hidden = false;
                    this.centerView.AddSubview(controller.View);
                    
                    if (barHidden) 
                        navController.NavigationBarHidden = false;
                    
                    this.addPanners();
                    this.applyShadowToSlidingView();
// todo:                    controller.vdc_viewDidAppear(false);
                };
            }
            
            // start the transition
            if (this.CenterController != null) 
            {
                currentFrame = this.CenterController.View.Frame;
                this.CenterController.WillMoveToParentViewController(null);

                if (centerController == this.LeftController) this.LeftController = null;
                if (centerController == this.RightController) this.RightController = null;


                beforeBlock(this.CenterController);

                try 
                {
                    this.CenterController.RemoveObserver(this, new NSString("title"));
                    if (this.automaticallyUpdateTabBarItems) 
                    {
                        this.CenterController.RemoveObserver(this, new NSString("tabBarItem.title"));
                        this.CenterController.RemoveObserver(this, new NSString("tabBarItem.image"));
                        this.CenterController.RemoveObserver(this, new NSString("hidesBottomBarWhenPushed"));
                    }
                }
                catch (Exception ex) 
                {
                    // gobble
                }

// todo:                this.centerController.setViewDeckController(null);
                this.CenterController.RemoveFromParentViewController();

                
                this.CenterController.DidMoveToParentViewController(null);
                // todo: dispose ? II_RELEASE(_centerController);
            }
            
            // make the switch
            this._centerController = centerController;
            
            if (this.CenterController != null) 
            {
                // and finish the transition
                //II_RETAIN(_centerController);
                this.AddChildViewController(this.CenterController);

// todo:                this.centerController.setViewDeckController(this);
                this.CenterController.AddObserver(this, new NSString("title"), 0, IntPtr.Zero);

                this.Title = this.CenterController.Title;

                if (this.automaticallyUpdateTabBarItems) 
                {
                    this.CenterController.AddObserver(this, new NSString("tabBarItem.title"), 0, IntPtr.Zero);
                    this.CenterController.AddObserver(this, new NSString("tabBarItem.image"), 0, IntPtr.Zero);
                    this.CenterController.AddObserver(this, new NSString("hidesBottomBarWhenPushed"), 0, IntPtr.Zero);
                    
                    this.TabBarItem.Title = this.CenterController.TabBarItem.Title;
                    this.TabBarItem.Image = this.CenterController.TabBarItem.Image;
                    this.HidesBottomBarWhenPushed = this.CenterController.HidesBottomBarWhenPushed;
                }
                
                afterBlock(this.CenterController);

                this.CenterController.DidMoveToParentViewController(this);
            }    
        }

        /// <summary>
        /// </summary>
        private void SetRightLedge(float rightLedge) 
        {
            // Compute the final ledge in two steps. This prevents a strange bug where
            // nesting MAX(X, MIN(Y, Z)) with miniscule referenceBounds returns a bogus near-zero value.

            float minLedge = Math.Min(this.referenceBounds.Size.Width, rightLedge);
            rightLedge = Math.Max(rightLedge, minLedge);

            if (this.viewAppeared && II_FLOAT_EQUAL(this.SlidingControllerView.Frame.Location.X, this.RightLedge - this.referenceBounds.Size.Width)) 
            {
                if (rightLedge < this.RightLedge) 
                {
                    UIView.Animate(CLOSE_SLIDE_DURATION(true), () =>
                    {
                        this.setSlidingFrameForOffset(rightLedge - this.referenceBounds.Size.Width);
                    });
                }
                else if (rightLedge > this.RightLedge) 
                {
                    UIView.Animate(OPEN_SLIDE_DURATION(true),() =>
                    {
                        this.setSlidingFrameForOffset(rightLedge - this.referenceBounds.Size.Width);
                    });
                }
            }

            this._rightLedge = rightLedge;
        }

        /// <summary>
        /// </summary>
        private void SetRightLedge(float rightLedge, Action<bool> completion)
        {
            // Compute the final ledge in two steps. This prevents a strange bug where
            // nesting MAX(X, MIN(Y, Z)) with miniscule referenceBounds returns a bogus near-zero value.

            float minLedge = Math.Min(this.referenceBounds.Size.Width, rightLedge);
            rightLedge = Math.Max(rightLedge, minLedge);

            if (this.viewAppeared && II_FLOAT_EQUAL(this.SlidingControllerView.Frame.Location.X, this.RightLedge - this.referenceBounds.Size.Width)) 
            {
                if (rightLedge < this.RightLedge) 
                {
                    UIView.Animate(CLOSE_SLIDE_DURATION(true), () =>
                    {
                        this.setSlidingFrameForOffset(rightLedge - this.referenceBounds.Size.Width);
                    }, () => completion(true));
                }
                else if (rightLedge > this.RightLedge) 
                {
                    UIView.Animate(OPEN_SLIDE_DURATION(true),() =>
                    {
                        this.setSlidingFrameForOffset(rightLedge - this.referenceBounds.Size.Width);
                    }, () => completion(true));
                }
            }

            this._rightLedge = rightLedge;
        }

        /// <summary>
        /// </summary>
        private void SetLeftLedge(float leftLedge) 
        {
            // Compute the final ledge in two steps. This prevents a strange bug where
            // nesting MAX(X, MIN(Y, Z)) with miniscule referenceBounds returns a bogus near-zero value.

            float minLedge = Math.Min(this.referenceBounds.Size.Width, leftLedge);
            leftLedge = Math.Max(leftLedge, minLedge);

            if (this.viewAppeared && II_FLOAT_EQUAL(this.SlidingControllerView.Frame.Location.X, this.referenceBounds.Size.Width - this.LeftLedge)) 
            {
                if (leftLedge < this.LeftLedge) 
                {
                    UIView.Animate(CLOSE_SLIDE_DURATION(true), () =>
                    {
                        this.setSlidingFrameForOffset(this.referenceBounds.Size.Width - leftLedge);
                    });
                }
                else if (leftLedge > this.LeftLedge) 
                {
                    UIView.Animate(OPEN_SLIDE_DURATION(true),() =>
                   {
                        this.setSlidingFrameForOffset(this.referenceBounds.Size.Width - leftLedge);
                    });
                }
            }

            this._leftLedge = leftLedge;
        }

        /// <summary>
        /// </summary>
        private void SetLeftLedge(float leftLedge, Action<bool> completion)
        {
            // Compute the final ledge in two steps. This prevents a strange bug where
            // nesting MAX(X, MIN(Y, Z)) with miniscule referenceBounds returns a bogus near-zero value.

            float minLedge = Math.Min(this.referenceBounds.Size.Width, leftLedge);
            leftLedge = Math.Max(leftLedge, minLedge);

            if (this.viewAppeared && II_FLOAT_EQUAL(this.SlidingControllerView.Frame.Location.X, this.referenceBounds.Size.Width - this.LeftLedge)) 
            {
                if (leftLedge < this.LeftLedge) 
                {
                    UIView.Animate(CLOSE_SLIDE_DURATION(true), () =>
                    {
                        this.setSlidingFrameForOffset(this.referenceBounds.Size.Width - leftLedge);
                    }, () => completion(true));
                }
                else if (leftLedge > this.LeftLedge) {
                    UIView.Animate(OPEN_SLIDE_DURATION(true),() =>
                    {
                        this.setSlidingFrameForOffset(this.referenceBounds.Size.Width - leftLedge);
                    }, () => completion(true));
                }
            }

            this._leftLedge = leftLedge;
        }

        private string __title;
        public override string Title
        {
            get
            {
                return this.CenterController.Title;
            }

            set
            {
                if (this.__title != value)
                {
                    this.__title = value;
                    base.Title = value;
                    this.CenterController.Title = value;
                }
            }
        }

        private void setAutomaticallyUpdateTabBarItems(bool automaticallyUpdateTabBarItems) 
        {
//            if (_automaticallyUpdateTabBarItems) {
//                @try {
//                    [_centerController removeObserver:self forKeyPath:@"tabBarItem.title"];
//                    [_centerController removeObserver:self forKeyPath:@"tabBarItem.image"];
//                    [_centerController removeObserver:self forKeyPath:@"hidesBottomBarWhenPushed"];
//                }
//                @catch (NSException *exception) {}
//            }
//            
//            _automaticallyUpdateTabBarItems = automaticallyUpdateTabBarItems;
//
//            if (_automaticallyUpdateTabBarItems) {
//                [_centerController addObserver:self forKeyPath:@"tabBarItem.title" options:0 context:nil];
//                [_centerController addObserver:self forKeyPath:@"tabBarItem.image" options:0 context:nil];
//                [_centerController addObserver:self forKeyPath:@"hidesBottomBarWhenPushed" options:0 context:nil];
//                self.tabBarItem.title = _centerController.tabBarItem.title;
//                self.tabBarItem.image = _centerController.tabBarItem.image;
//            }
        }


        #endregion

        private bool setSlidingAndReferenceViews() 
        {
            if (this.NavigationController != null && this.NavigationControllerBehavior == ViewDeckNavigationControllerBehavior.Integrated) 
            {
                if (this.NavigationController.View.Superview != null) 
                {
                    this.slidingController = this.NavigationController;
                    this.referenceView = this.NavigationController.View.Superview;
                    return true;
                }
            }
            else 
            {
                this.slidingController = this.CenterController;
                this.referenceView = this.View;
                return true;
            }
            
            return false;
        }

        //#pragma mark - observation

        public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
        {
//            if (object == _centerController) {
//                if ([@"tabBarItem.title" isEqualToString:keyPath]) {
//                    self.tabBarItem.title = _centerController.tabBarItem.title;
//                    return;
//                }
//                
//                if ([@"tabBarItem.image" isEqualToString:keyPath]) {
//                    self.tabBarItem.image = _centerController.tabBarItem.image;
//                    return;
//                }
//
//                if ([@"hidesBottomBarWhenPushed" isEqualToString:keyPath]) {
//                    self.hidesBottomBarWhenPushed = _centerController.hidesBottomBarWhenPushed;
//                    self.tabBarController.hidesBottomBarWhenPushed = _centerController.hidesBottomBarWhenPushed;
//                    return;
//                }
//            }
//
//            if ([@"title" isEqualToString:keyPath]) {
//                if (!II_STRING_EQUAL([super title], self.centerController.title)) {
//                    self.title = self.centerController.title;
//                }
//                return;
//            }
//            
//            if ([keyPath isEqualToString:@"bounds"]) {
//                CGFloat offset = self.slidingControllerView.Frame.Location.X;
//                [self setSlidingFrameForOffset:offset];
//                self.slidingControllerView.layer.shadowPath = [UIBezierPath bezierPathWithRect:self.referenceBounds].CGPath;
//                UINavigationController* navController = [self.centerController isKindOfClass:[UINavigationController class]] 
//                ? (UINavigationController*)self.centerController 
//                : nil;
//                if (navController != nil && !navController.navigationBarHidden) {
//                    navController.navigationBarHidden = YES;
//                    navController.navigationBarHidden = NO;
//                }
//                return;
//            }
        }

       // #pragma mark - Shadow

        private void restoreShadowToSlidingView() 
        {
//            UIView* shadowedView = self.slidingControllerView;
//            if (!shadowedView) return;
//            
//            shadowedView.layer.shadowRadius = self.LocationalShadowRadius;
//            shadowedView.layer.shadowOpacity = self.LocationalShadowOpacity;
//            shadowedView.layer.shadowColor = [self.LocationalShadowColor CGColor]; 
//            shadowedView.layer.shadowOffset = self.LocationalShadowOffset;
//            shadowedView.layer.shadowPath = [self.LocationalShadowPath CGPath];
        }

        private void applyShadowToSlidingView() 
        {
            UIView shadowedView = this.SlidingControllerView;
            if (shadowedView == null) return;
            
            this.originalShadowRadius = shadowedView.Layer.ShadowRadius;
            this.originalShadowOpacity = shadowedView.Layer.ShadowOpacity;
            this.originalShadowColor = shadowedView.Layer.ShadowColor != null ? UIColor.FromCGColor(this.SlidingControllerView.Layer.ShadowColor) : null;
            this.originalShadowOffset = shadowedView.Layer.ShadowOffset;
//            this.originalShadowPath = shadowedView.Layer.ShadowPath  != null  ? UIBezierPath.FromPath(this.slidingControllerView.Layer.ShadowPath) : null;
            
//            if ([this.delegate respondsToSelector:@selector(viewDeckController:applyShadow:withBounds:)]) 
//            {
//                [this.delegate viewDeckController:this applyShadow:shadowedView.layer withBounds:self.referenceBounds];
//            }
//            else 
            {
                shadowedView.Layer.MasksToBounds = false;
                shadowedView.Layer.ShadowRadius = 10;
                shadowedView.Layer.ShadowOpacity = 0.5f;
                shadowedView.Layer.ShadowColor = UIColor.Black.CGColor;
                shadowedView.Layer.ShadowOffset = SizeF.Empty;
                shadowedView.Layer.ShadowPath = UIBezierPath.FromRect(shadowedView.Bounds).CGPath;
            }
        }

         

    }


}
