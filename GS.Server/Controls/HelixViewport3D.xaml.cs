using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Media3D;

namespace GS.Server.Controls
{
    /// <summary>
    /// Interaction logic for HelixViewport3D.xaml.
    /// Camera sync is managed here because PerspectiveCamera is a Freezable outside
    /// the logical tree, making TwoWay XAML bindings on it unsupported by WPF.
    /// </summary>
    public partial class HelixViewport3D
    {
        private PerspectiveCamera _camera;
        private bool _syncingCamera;
        private PropertyInfo _lookDirectionProp;
        private PropertyInfo _upDirectionProp;
        private PropertyInfo _positionProp;

        public HelixViewport3D()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _camera = Viewport3d.Camera as PerspectiveCamera;
            SyncCameraFromViewModel();
            AttachCameraWatchers();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachCameraWatchers();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Clear cached reflection handles so they are re-resolved for the new DataContext type.
            _lookDirectionProp = null;
            _upDirectionProp = null;
            _positionProp = null;
            if (IsLoaded)
                SyncCameraFromViewModel();
        }

        /// <summary>
        /// Caches reflection handles for the ViewModel's camera properties on first use.
        /// </summary>
        private void EnsureVmPropertiesCached()
        {
            if (DataContext == null || _lookDirectionProp != null) return;
            var vmType = DataContext.GetType();
            _lookDirectionProp = vmType.GetProperty("LookDirection");
            _upDirectionProp = vmType.GetProperty("UpDirection");
            _positionProp = vmType.GetProperty("Position");
        }

        /// <summary>
        /// Pushes current ViewModel camera values onto the PerspectiveCamera.
        /// </summary>
        private void SyncCameraFromViewModel()
        {
            if (_camera == null || DataContext == null) return;
            EnsureVmPropertiesCached();
            _syncingCamera = true;
            try
            {
                if (_lookDirectionProp != null) _camera.LookDirection = (Vector3D)_lookDirectionProp.GetValue(DataContext);
                if (_upDirectionProp != null) _camera.UpDirection = (Vector3D)_upDirectionProp.GetValue(DataContext);
                if (_positionProp != null) _camera.Position = (Point3D)_positionProp.GetValue(DataContext);
            }
            finally
            {
                _syncingCamera = false;
            }
        }

        private void AttachCameraWatchers()
        {
            if (_camera == null) return;
            DependencyPropertyDescriptor.FromProperty(ProjectionCamera.LookDirectionProperty, typeof(ProjectionCamera)).AddValueChanged(_camera, OnCameraPropertyChanged);
            DependencyPropertyDescriptor.FromProperty(ProjectionCamera.UpDirectionProperty, typeof(ProjectionCamera)).AddValueChanged(_camera, OnCameraPropertyChanged);
            DependencyPropertyDescriptor.FromProperty(ProjectionCamera.PositionProperty, typeof(ProjectionCamera)).AddValueChanged(_camera, OnCameraPropertyChanged);
        }

        private void DetachCameraWatchers()
        {
            if (_camera == null) return;
            DependencyPropertyDescriptor.FromProperty(ProjectionCamera.LookDirectionProperty, typeof(ProjectionCamera)).RemoveValueChanged(_camera, OnCameraPropertyChanged);
            DependencyPropertyDescriptor.FromProperty(ProjectionCamera.UpDirectionProperty, typeof(ProjectionCamera)).RemoveValueChanged(_camera, OnCameraPropertyChanged);
            DependencyPropertyDescriptor.FromProperty(ProjectionCamera.PositionProperty, typeof(ProjectionCamera)).RemoveValueChanged(_camera, OnCameraPropertyChanged);
        }

        /// <summary>
        /// Fires when HelixToolkit's camera controller updates any of the three camera DPs.
        /// Writes the new values back to the ViewModel so SaveModelViewCmd reads current state.
        /// </summary>
        private void OnCameraPropertyChanged(object sender, EventArgs e)
        {
            if (_syncingCamera || _camera == null || DataContext == null) return;
            EnsureVmPropertiesCached();
            _lookDirectionProp?.SetValue(DataContext, _camera.LookDirection);
            _upDirectionProp?.SetValue(DataContext, _camera.UpDirection);
            _positionProp?.SetValue(DataContext, _camera.Position);
        }
    }
}
