﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using Microsoft.Expression.Interactivity.Core;

// Lovingly borrowed from http://www.codeproject.com/Articles/271082/Use-and-DataTemplate-as-an-Adorner

namespace SeeGit
{
    /// <summary>
    /// Custom Adorner hosting a ContentControl with a ContentTemplate
    /// </summary>
    internal class CaTemplatedAdorner : Adorner
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="adornedElement"></param>
        public CaTemplatedAdorner(UIElement adornedElement, FrameworkElement frameworkElementAdorner)
            : base(adornedElement)
        {
            // Assure we get mouse hits
            _frameworkElementAdorner = frameworkElementAdorner;
            AddVisualChild(_frameworkElementAdorner);
            AddLogicalChild(_frameworkElementAdorner);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            return _frameworkElementAdorner;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            //_frameworkElementAdorner.Width = constraint.Width;
            //_frameworkElementAdorner.Height = constraint.Height;
            _frameworkElementAdorner.Measure(constraint);

            return _frameworkElementAdorner.DesiredSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="finalSize"></param>
        /// <returns></returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            _frameworkElementAdorner.Arrange(new Rect(new Point(0, 0), finalSize));
            return finalSize;
        }


        /// <summary>
        /// 
        /// </summary>
        private readonly FrameworkElement _frameworkElementAdorner;
    }


    /// <summary>
    /// Behavior managing an adorner datatemplate
    /// </summary>
    public class CaAdornerBehavior : Behavior<DependencyObject>
    {
        /// <summary>
        /// Custom Adorner class
        /// </summary>
        private CaTemplatedAdorner _caTemplatedAdorner;

        /// <summary>
        /// Adorner control holder. This object is passed to the Adorner
        /// </summary>
        private ContentControl _adornerControl;

        /// <summary>
        /// Preset actions to handle delayed contruction/destruction
        /// </summary>
        private Func<bool> _delayedFactory;

        private Func<bool> _delayedDestruction;
        private Func<bool> _nonDelayedFactory;
        private Func<bool> _nonDelayedDestruction;

        /// <summary>
        /// Preset actions
        /// </summary>
        private readonly Func<bool> _factor;

        private readonly Func<bool> _dispose;
        private readonly Func<bool> _emptyAction;

        /// <summary>
        /// 
        /// </summary>
        public CaAdornerBehavior()
        {
            //
            // create three static Actions to work with delayed, or not, construction
            //
            _emptyAction = () => false;

            //
            // Delay factory action
            //
            _factor = () =>
                      {
                          if (AdornerTemplate != null)
                          {
                              var associated = AssociatedObject as UIElement;
                              if (associated == null) return false;
                              var adornerLayer = AdornerLayer.GetAdornerLayer(associated);

                              if (null == adornerLayer)
                                  throw new NullReferenceException(
                                      string.Format("No adorner found in attached object: {0}", AssociatedObject));

                              // Create adorner
                              _adornerControl = new ContentControl();

                              // Add to adorner
                              adornerLayer.Add(
                                  _caTemplatedAdorner =
                                  new CaTemplatedAdorner(AssociatedObject as UIElement, _adornerControl));

                              // set realted bindings
                              _adornerControl.Content = AdornerTemplate.LoadContent();
                              _adornerControl.Visibility = AdornerVisible;

                              // Bind internal dependency to external 
                              var bindingMargin = new Binding("AdornerMargin");
                              bindingMargin.Source = this;
                              BindingOperations.SetBinding(_caTemplatedAdorner, FrameworkElement.MarginProperty,
                                                           bindingMargin);
                          }

                          return true;
                      };

            //
            // proper dispose
            //
            _dispose = () =>
                       {
                           if (null != _caTemplatedAdorner)
                           {
                               var associated = AssociatedObject as UIElement;
                               if (associated == null) return false;
                               var adornerLayer = AdornerLayer.GetAdornerLayer(associated);
                               adornerLayer.Remove(_caTemplatedAdorner);
                               BindingOperations.ClearBinding(_caTemplatedAdorner, FrameworkElement.MarginProperty);
                               _caTemplatedAdorner = null;
                               _adornerControl = null;
                           }
                           return true;
                       };

            // set intial actions 
            SetDelayedState(DelayConstruction);

            // Behavior events
            ShowAdornerCommand = new ActionCommand(ShowAdorner);
            HideAdornerCommand = new ActionCommand(HideAdorner);
        }

        /// <summary>
        /// Standard behavior OnAttached() override
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            _nonDelayedFactory();
        }


        /// <summary>
        /// Standard behavior OnDetaching() override
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            _nonDelayedDestruction();
        }


        /// <summary>
        /// ShowAdorner
        /// </summary>
        private void ShowAdorner(object parameter)
        {
            _delayedFactory();

            // Set Data context here because default template assigment is  not setting the context
            var frameworkElement = AssociatedObject as FrameworkElement;
            if (frameworkElement != null)
            {
                var dtContext = frameworkElement.DataContext;
                if (null == _adornerControl.DataContext)
                    _adornerControl.DataContext = dtContext;
            }

            _adornerControl.Visibility = Visibility.Visible;
        }


        /// <summary>
        /// HideAdorner
        /// </summary>
        private void HideAdorner(object parameter)
        {
            if (!_delayedDestruction())
            {
                if (_adornerControl.IsMouseOver)
                {
                    _adornerControl.MouseLeave -= (s, e) => _adornerControl.Visibility = AdornerVisible;
                    _adornerControl.MouseLeave += (s, e) => _adornerControl.Visibility = AdornerVisible;
                }
                else
                {
                    _adornerControl.Visibility = AdornerVisible;
                }
            }
        }

        /// <summary>
        /// ShowAdornerCommand
        /// </summary>
        public ICommand ShowAdornerCommand { get; private set; }


        /// <summary>
        /// HideAdornerCommand
        /// </summary>
        public ICommand HideAdornerCommand { get; private set; }


        /// <summary>
        /// Template to use in adorner. The template is hosted as a content control template with full 
        /// HitTest.
        /// </summary>
        public static readonly DependencyProperty AdornerTemplateProperty = DependencyProperty.Register(
            "AdornerTemplate",
            typeof (DataTemplate),
            typeof (CaAdornerBehavior), new PropertyMetadata((d, o) =>
                                                             {
                                                                 if (null != ((CaAdornerBehavior)d)._adornerControl)
                                                                     ((CaAdornerBehavior)d)._adornerControl.
                                                                         ContentTemplate = (DataTemplate)o.NewValue;
                                                             }));

        /// <summary>
        /// Data template for the adroner. Used inside a ContentControl. 
        /// </summary>
        public DataTemplate AdornerTemplate
        {
            get { return (DataTemplate)GetValue(AdornerTemplateProperty); }
            set { SetValue(AdornerTemplateProperty, value); }
        }


        /// <summary>
        /// Adorner Margin
        /// </summary>
        public static readonly DependencyProperty AdornerMarginProperty = DependencyProperty.Register(
            "AdornerMargin",
            typeof (Thickness),
            typeof (CaAdornerBehavior)
            );

        /// <summary>
        /// Adorner Margin
        /// </summary>
        public Thickness AdornerMargin
        {
            get { return (Thickness)GetValue(AdornerMarginProperty); }
            set { SetValue(AdornerMarginProperty, value); }
        }


        /// <summary>
        /// AdornerVisibleProperty
        /// </summary>
        public static readonly DependencyProperty AdornerVisibleProperty = DependencyProperty.Register(
            "AdornerVisible",
            typeof (Visibility),
            typeof (CaAdornerBehavior),
            new PropertyMetadata(Visibility.Hidden, (d, o) =>
                                                    {
                                                        if (null != ((CaAdornerBehavior)d)._adornerControl)
                                                            ((CaAdornerBehavior)d)._adornerControl.Visibility =
                                                                (Visibility)o.NewValue;
                                                    }));

        /// <summary>
        /// Data template for the adroner. Used inside a ContentControl. 
        /// </summary>
        public Visibility AdornerVisible
        {
            get { return (Visibility)GetValue(AdornerVisibleProperty); }
            set { SetValue(AdornerVisibleProperty, value); }
        }

        /// <summary>
        /// True = Construct and dispose adorner on demand. Slower user experience.
        /// False = (Default) Create aodrner when attached. Faster user experience.
        /// </summary>
        public static readonly DependencyProperty DelayConstructionProperty = DependencyProperty.Register(
            "DelayConstruction",
            typeof (bool),
            typeof (CaAdornerBehavior),
            new PropertyMetadata(false, (d, o) =>
                                        {
                                            var adorner = d as CaAdornerBehavior;
                                            if (d == null) return;
                                            if (adorner != null) adorner.SetDelayedState((bool)o.NewValue);
                                        }));

        /// <summary>
        /// Data template for the adroner. Used inside a ContentControl. 
        /// </summary>
        public bool DelayConstruction
        {
            get { return (bool)GetValue(DelayConstructionProperty); }
        }

        /// <summary>
        /// Data template for the adroner. Used inside a ContentControl. 
        /// </summary>
        /// <param name="delayed"></param>
        private void SetDelayedState(bool delayed)
        {
            _delayedFactory = delayed ? _factor : _emptyAction;
            _delayedDestruction = delayed ? _dispose : _emptyAction;
            _nonDelayedFactory = !delayed ? _factor : _emptyAction;
            _nonDelayedDestruction = !delayed ? _dispose : _emptyAction;
        }

        public object DataContext
        {
            get { return GetValue(DataContextProperty); }
            set { SetValue(DataContextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DataContext.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataContextProperty =
            DependencyProperty.Register("DataContext", typeof (object), typeof (CaAdornerBehavior),
                                        new UIPropertyMetadata(OnDataContextChanged));

        private static void OnDataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var adorner = d as CaAdornerBehavior;
            if (adorner == null || adorner._adornerControl == null) return;

            adorner._adornerControl.DataContext = adorner.DataContext;
        }
    }
}