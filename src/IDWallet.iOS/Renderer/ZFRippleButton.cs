using CoreAnimation;
using CoreGraphics;
using Foundation;
using System;
using UIKit;

namespace FormsPinView.iOS
{
    public class ZFRippleButton : UIButton
    {
        public float _ripplePercent = 0.8f;
        private readonly UIView _rippleBackgroundView = new UIView();
        private readonly UIView _rippleView = new UIView();
        private float _buttonCornerRadius = 0f;

        private UIColor _rippleBackgroundColor;

        private UIColor _rippleColor;

        private float _tempShadowOpacity = 0f;

        private nfloat _tempShadowRadius = 0f;

        private CGPoint? _touchCenterLocation;

        public ZFRippleButton(CGRect frame) : base(frame)
        {
            Setup();
        }

        public float ButtonCornerRadius
        {
            get => _buttonCornerRadius;
            set
            {
                _buttonCornerRadius = value;
                Layer.CornerRadius = value;
            }
        }

        public UIColor RippleBackgroundColor
        {
            get => _rippleBackgroundColor;
            set
            {
                _rippleBackgroundColor = value;
                _rippleView.BackgroundColor = value;
            }
        }

        public UIColor RippleColor
        {
            get => _rippleColor;
            set
            {
                _rippleColor = value;
                _rippleView.BackgroundColor = RippleColor;
            }
        }

        public bool RippleOverBounds { get; set; } = false;

        public float RipplePercent
        {
            get => _ripplePercent;
            set
            {
                _ripplePercent = value;
                SetupRippleView();
            }
        }
        public bool ShadowRippleEnable { get; set; } = true;
        public float ShadowRippleRadius { get; set; } = 1f;
        public double TouchUpAnimationTime { get; set; } = 0.6d;
        public bool TrackTouchLocation { get; set; } = false;
        private CAShapeLayer RippleMask
        {
            get
            {
                if (!RippleOverBounds)
                {
                    CAShapeLayer maskLayer = new CAShapeLayer();
                    maskLayer.Path = UIBezierPath.FromRoundedRect(Bounds, Layer.CornerRadius).CGPath;
                    return maskLayer;
                }
                else
                {
                    return null;
                }
            }
        }
        public override bool BeginTracking(UITouch touch, UIEvent uievent)
        {
            if (TrackTouchLocation)
            {
                _touchCenterLocation = touch.LocationInView(this);
            }
            else
            {
                _touchCenterLocation = null;
            }

            UIView.Animate(0.1, 0, UIViewAnimationOptions.AllowUserInteraction, () =>
            {
                _rippleBackgroundView.Alpha = 1;
            }, null);
            _rippleView.Transform = CGAffineTransform.MakeScale(0.5f, 0.5f);

            UIView.Animate(0.7, 0, UIViewAnimationOptions.CurveEaseOut | UIViewAnimationOptions.AllowUserInteraction, () =>
            {
                _rippleView.Transform = CGAffineTransform.MakeIdentity();
            }, null);

            if (ShadowRippleEnable)
            {
                _tempShadowRadius = Layer.ShadowRadius;
                _tempShadowOpacity = Layer.ShadowOpacity;

                CABasicAnimation shadowAnim = new CABasicAnimation { KeyPath = "shadowRadius" };
                shadowAnim.To = NSValue.FromObject(ShadowRippleRadius);

                CABasicAnimation opacityAnim = new CABasicAnimation { KeyPath = "shadowOpacity" };
                opacityAnim.To = NSValue.FromObject(1);

                CAAnimationGroup groupAnim = new CAAnimationGroup();
                groupAnim.Duration = 0.7;
                groupAnim.FillMode = CAFillMode.Forwards;
                groupAnim.RemovedOnCompletion = false;
                groupAnim.Animations = new[] { shadowAnim, opacityAnim };
                Layer.AddAnimation(groupAnim, "shadow");
            }
            return base.BeginTracking(touch, uievent);
        }

        public override void CancelTracking(UIEvent uievent)
        {
            base.CancelTracking(uievent);
            AnimateToNormal();
        }

        public override void EndTracking(UITouch uitouch, UIEvent uievent)
        {
            base.EndTracking(uitouch, uievent);
            AnimateToNormal();
        }

        private void AnimateToNormal()
        {
            UIView.Animate(0.1, 0, UIViewAnimationOptions.AllowUserInteraction, () =>
            {
                _rippleBackgroundView.Alpha = 1;
            }, () =>
            {
                UIView.Animate(TouchUpAnimationTime, 0, UIViewAnimationOptions.AllowUserInteraction, () =>
                {
                    _rippleBackgroundView.Alpha = 0;
                }, null);
            });

            UIView.Animate(0.7, 0, UIViewAnimationOptions.CurveEaseOut | UIViewAnimationOptions.BeginFromCurrentState | UIViewAnimationOptions.AllowUserInteraction, () =>
            {
                _rippleView.Transform = CGAffineTransform.MakeIdentity();

                CABasicAnimation shadowAnim = new CABasicAnimation { KeyPath = "shadowRadius" };
                shadowAnim.To = NSObject.FromObject(_tempShadowRadius);

                CABasicAnimation opacityAnim = new CABasicAnimation { KeyPath = "shadowOpacity" };
                opacityAnim.To = NSObject.FromObject(_tempShadowOpacity);

                CAAnimationGroup groupAnim = new CAAnimationGroup();
                groupAnim.Duration = 0.7;
                groupAnim.FillMode = CAFillMode.Forwards;
                groupAnim.RemovedOnCompletion = false;
                groupAnim.Animations = new[] { shadowAnim, opacityAnim };

                Layer.AddAnimation(groupAnim, "shadowBack");
            }, null);
        }

        private void Setup()
        {
            SetupRippleView();

            _rippleBackgroundView.BackgroundColor = RippleBackgroundColor;
            _rippleBackgroundView.Frame = Bounds;

            _rippleBackgroundView.AddSubview(_rippleView);
            AddSubview(_rippleBackgroundView);

            _rippleBackgroundView.Alpha = 0;

            Layer.ShadowRadius = 0;
            Layer.ShadowOffset = new CGSize(width: 0, height: 1);
            Layer.ShadowColor = new UIColor(white: 0.0f, alpha: 0.5f).CGColor;
        }

        private void SetupRippleView()
        {
            nfloat size = Bounds.Width * RipplePercent;
            nfloat x = (Bounds.Width / 2) - (size / 2);
            nfloat y = (Bounds.Height / 2) - (size / 2);
            nfloat corner = size / 2;

            _rippleView.BackgroundColor = RippleColor;
            _rippleView.Frame = new CGRect(x, y, size, size);
            _rippleView.Layer.CornerRadius = corner;
        }
    }
}

