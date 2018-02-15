using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts.Core.Abstractions;
using LiveCharts.Core.Charts;
using LiveCharts.Core.DataSeries;
using LiveCharts.Core.Dimensions;
using LiveCharts.Core.Drawing;
using LiveCharts.Core.Events;
using LiveCharts.Wpf.Controls;
using LiveCharts.Wpf.Interaction;
using DataInteractionHandler = LiveCharts.Core.Events.DataInteractionHandler;
using Size = LiveCharts.Core.Drawing.Size;

namespace LiveCharts.Wpf
{
    /// <summary>
    /// Defines a chart class.
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Canvas" />
    /// <seealso cref="LiveCharts.Core.Abstractions.IChartView" />
    /// <seealso cref="LiveCharts.Core.Abstractions.IDesktopChart" />
    public abstract class Chart : Canvas, IChartView, IDesktopChart
    {
        /// <summary>
        /// Initializes the <see cref="Chart"/> class.
        /// </summary>
        static Chart()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(Chart),
                new FrameworkPropertyMetadata(typeof(Chart)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chart"/> class.
        /// </summary>
        protected Chart()
        {
            DrawMargin = Core.Drawing.Margin.Empty;
            DrawArea = new Canvas
            {
                Background = Brushes.Transparent // to detect events
            };
            TooltipPopup = new Popup
            {
                AllowsTransparency = true,
                Placement = PlacementMode.RelativePoint
            };
            Children.Add(DrawArea);
            Children.Add(TooltipPopup);
            SetValue(LegendProperty, new ChartLegend());
            SetValue(DataTooltipProperty, new ChartToolTip());
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += (sender, args) =>
            {
                var p = args.GetPosition(DrawArea);
                var points = Model.GetInteractedPoints(p.X, p.Y);
                var e = new DataInteractionEventArgs(args.MouseDevice, args.Timestamp, args.ChangedButton, points)
                {
                    RoutedEvent = MouseDownEvent
                };
                OnDataClick(e);
            };
            MouseLeftButtonDown += (sender, args) =>
            {
                if (args.ClickCount != 2) return;
                var p = args.GetPosition(DrawArea);
                var points = Model.GetInteractedPoints(p.X, p.Y);
                var e = new DataInteractionEventArgs(args.MouseDevice, args.Timestamp, args.ChangedButton, points)
                {
                    RoutedEvent = MouseDownEvent
                };
                OnDataClick(e);
            };
        }

        #region Dependency properties

        /// <summary>
        /// The series property.
        /// </summary>
        public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
            nameof(Series), typeof(IEnumerable<BaseSeries>), typeof(Chart),
            new PropertyMetadata(null, RaiseOnPropertyChanged(nameof(Series))));

        /// <summary>
        /// The animations speed property, default value is 250 milliseconds.
        /// </summary>
        public static readonly DependencyProperty AnimationsSpeedProperty = DependencyProperty.Register(
            nameof(AnimationsSpeed), typeof(TimeSpan), typeof(Chart),
            new PropertyMetadata(TimeSpan.FromMilliseconds(250), RaiseOnPropertyChanged(nameof(AnimationsSpeed))));

        /// <summary>
        /// The tooltip timeout property
        /// </summary>
        public static readonly DependencyProperty TooltipTimeoutProperty = DependencyProperty.Register(
            nameof(TooltipTimeOut), typeof(TimeSpan), typeof(Chart),
            new PropertyMetadata(TimeSpan.FromMilliseconds(150)));

        /// <summary>
        /// The legend property, default is DefaultLegend class.
        /// </summary>
        public static readonly DependencyProperty LegendProperty = DependencyProperty.Register(
            nameof(Legend), typeof(ILegend), typeof(Chart),
            new PropertyMetadata(null, RaiseOnPropertyChanged(nameof(Legend))));

        /// <summary>
        /// The legend position property
        /// </summary>
        public static readonly DependencyProperty LegendPositionProperty = DependencyProperty.Register(
            nameof(LegendPosition), typeof(LegendPosition), typeof(Chart),
            new PropertyMetadata(LegendPosition.None, RaiseOnPropertyChanged(nameof(LegendPosition))));

        /// <summary>
        /// The data tooltip property.
        /// </summary>
        public static readonly DependencyProperty DataTooltipProperty = DependencyProperty.Register(
            nameof(DataToolTip), typeof(IDataToolTip), typeof(Chart),
            new PropertyMetadata(null));

        #endregion

        #region Properties

        /// <summary>
        /// Gets the draw area.
        /// </summary>
        /// <value>
        /// The draw area.
        /// </value>
        public Canvas DrawArea { get; }

        /// <summary>
        /// Gets or sets the tooltip popup.
        /// </summary>
        /// <value>
        /// The tooltip popup.
        /// </value>
        public Popup TooltipPopup { get; set; }

        #endregion

        #region private and protected methods

        /// <summary>
        /// Gets the planes in the current chart.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual IList<IList<Plane>> GetPlanes()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Notifies that the specified property changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        protected static PropertyChangedCallback RaiseOnPropertyChanged(string propertyName)
        {
            return (sender, eventArgs) =>
            {
                var chart = (Chart)sender;
                chart.OnPropertyChanged(propertyName);
            };
        }

        /// <summary>
        /// Raises the <see cref="E:DataClick" /> event.
        /// </summary>
        /// <param name="args">The <see cref="DataInteractionEventArgs"/> instance containing the event data.</param>
        protected virtual void OnDataClick(DataInteractionEventArgs args)
        {
            DataClick?.Invoke(this, args);
            if (DataClickCommand != null && DataClickCommand.CanExecute(args.Points))
            {
                DataClickCommand.Execute(args.Points);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:DataDoubleClick" /> event.
        /// </summary>
        /// <param name="args">The <see cref="DataInteractionEventArgs"/> instance containing the event data.</param>
        protected virtual void OnDataDoubleClick(DataInteractionEventArgs args)
        {
            DataDoubleClick?.Invoke(this, args);
            if (DataClickCommand != null && DataDoubleClickCommand.CanExecute(args.Points))
            {
                DataClickCommand.Execute(args.Points);
            }
        }

        private void OnLoaded(object sender, EventArgs eventArgs)
        {
            ChartViewLoaded?.Invoke(this);
        }

        private void OnSizeChanged(object o, SizeChangedEventArgs sizeChangedEventArgs)
        {
            ChartViewResized?.Invoke(this);
        }

        private void OnMouseMove(object o, MouseEventArgs args)
        {
            if (DataToolTip == null) return;
            var point = args.GetPosition(DrawArea);
            PointerMoved?.Invoke(new Core.Drawing.Point(point.X, point.Y), DataToolTip.SelectionMode, point.X, point.Y);
        }

        #endregion

        #region IChartView implementation

        private event ChartEventHandler ChartViewLoaded;

        event ChartEventHandler IChartView.ChartViewLoaded
        {
            add => ChartViewLoaded += value;
            remove => ChartViewLoaded -= value;
        }

        private event ChartEventHandler ChartViewResized;

        event ChartEventHandler IChartView.ChartViewResized
        {
            add => ChartViewResized += value;
            remove => ChartViewResized -= value;
        }

        private event PointerMovedHandler PointerMoved;

        event PointerMovedHandler IChartView.PointerMoved
        {
            add => PointerMoved += value;
            remove => PointerMoved -= value;
        }

        /// <inheritdoc />
        public event ChartEventHandler UpdatePreview
        {
            add => Model.UpdatePreview += value;
            remove => Model.UpdatePreview -= value;
        }

        /// <inheritdoc cref="IChartView.UpdatePreview" />
        public ICommand UpdatePreviewCommand
        {
            get => Model.UpdatePreviewCommand;
            set => Model.UpdatePreviewCommand = value;
        }

        /// <inheritdoc />
        public event ChartEventHandler Updated
        {
            add => Model.Updated += value;
            remove => Model.Updated -= value;
        }

        /// <inheritdoc cref="IChartView.Updated" />
        public ICommand UpdatedCommand
        {
            get => Model.UpdatedCommand;
            set => Model.UpdatedCommand = value;
        }

        /// <inheritdoc cref="IChartView.Model"/>
        public ChartModel Model { get; protected set; }

        /// <inheritdoc cref="IChartView.ControlSize"/>
        Size IChartView.ControlSize => new Size((int)ActualWidth, (int)ActualHeight);

        /// <inheritdoc cref="IChartView.DrawMargin"/>
        public Margin DrawMargin { get; set; }

        /// <inheritdoc />
        IList<IList<Plane>> IChartView.Dimensions => GetPlanes();

        /// <inheritdoc cref="IChartView.Series"/>
        public IEnumerable<BaseSeries> Series
        {
            get => (IEnumerable<BaseSeries>)GetValue(SeriesProperty);
            set => SetValue(SeriesProperty, value);
        }

        /// <inheritdoc cref="IChartView.AnimationsSpeed"/>
        public TimeSpan AnimationsSpeed
        {
            get => (TimeSpan)GetValue(AnimationsSpeedProperty);
            set => SetValue(AnimationsSpeedProperty, value);
        }

        /// <inheritdoc />
        public TimeSpan TooltipTimeOut
        {
            get => (TimeSpan)GetValue(TooltipTimeoutProperty);
            set => SetValue(TooltipTimeoutProperty, value);
        }

        /// <inheritdoc cref="IChartView.Legend"/>
        public ILegend Legend
        {
            get => (ILegend)GetValue(LegendProperty);
            set => SetValue(LegendProperty, value);
        }

        /// <inheritdoc cref="IChartView.LegendPosition"/>
        public LegendPosition LegendPosition
        {
            get => (LegendPosition)GetValue(LegendPositionProperty);
            set => SetValue(LegendPositionProperty, value);
        }

        /// <inheritdoc cref="IChartView.DataToolTip"/>
        public IDataToolTip DataToolTip
        {
            get => (IDataToolTip)GetValue(DataTooltipProperty);
            set => SetValue(DataTooltipProperty, value);
        }

        /// <inheritdoc cref="IChartView.UpdateDrawArea"/>
        public void UpdateDrawArea(Rectangle model)
        {
            SetLeft(DrawArea, model.Left);
            SetTop(DrawArea, model.Top);
            DrawArea.Width = model.Width;
            DrawArea.Height = model.Height;
        }

        void IChartView.InvokeOnUiThread(Action action)
        {
            Dispatcher.Invoke(action);
        }

        #endregion

        #region PlatformSpecific events 

        ///<summary>
        /// Occurs when the user clicks in a data point.
        /// </summary>
        public event Interaction.DataInteractionHandler DataClick;

        ///<summary>
        /// Occurs when the user clicks in a data point.
        /// </summary>
        public ICommand DataClickCommand { get; set; }

        ///<summary>
        /// Occurs when the user double clicks in a data point.
        /// </summary>
        public event Interaction.DataInteractionHandler DataDoubleClick;

        ///<summary>
        /// Occurs when the user double clicks in a data point.
        /// </summary>
        public ICommand DataDoubleClickCommand { get; set; }

        #endregion

        #region IDesktopChart implementation

        /// <inheritdoc />
        public event DataInteractionHandler DataMouseEnter
        {
            add => Model.DataPointerEnter += value;
            remove => Model.DataPointerLeave -= value;
        }

        public ICommand DataMouseEnterCommand
        {
            get => Model.DataPointerEnterCommand;
            set => Model.DataPointerEnterCommand = value;
        }

        /// <inheritdoc />
        public event DataInteractionHandler DataMouseLeave
        {
            add => Model.DataPointerLeave += value;
            remove => Model.DataPointerLeave -= value;
        }

        public ICommand DataMouseLeaveCommand
        {
            get => Model.DataPointerLeaveCommand;
            set => Model.DataPointerLeaveCommand = value;
        }

        #endregion

        #region INPC implementation

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}