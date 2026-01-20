using System.Windows;
using System.Windows.Input;

namespace Rotatonator
{
    public partial class OverlayAnchor : Window
    {
        public OverlayAnchor()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        public Point GetPosition()
        {
            return new Point(this.Left, this.Top);
        }

        public void SetPosition(Point position)
        {
            this.Left = position.X;
            this.Top = position.Y;
        }
    }
}
