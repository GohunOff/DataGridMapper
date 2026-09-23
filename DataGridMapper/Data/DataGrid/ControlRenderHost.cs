using System.Drawing;
using System.Windows.Forms;

namespace MC.Data.DataGrid
{
    public class ControlRenderHost : Panel
    {
        private Control _currentControl;

        public ControlRenderHost()
        {
            Location = new Point(-10000, -10000);
            Size = new Size(1, 1);

            Visible = true;
            AutoScroll = false;
        }

        private void AttachControl(Control control)
        {
            if (_currentControl == control)
                return;

            if (_currentControl != null)
            {
                Controls.Remove(_currentControl);
            }

            _currentControl = control;

            control.Location = Point.Empty;

            Controls.Add(control);
        }

        public Bitmap Render(Control control, Size size)
        {
            AttachControl (control);

            control.Size = size;

            PerformLayout();
            control.PerformLayout();
            control.Update();

            var bitmap = new Bitmap(
                size.Width,
                size.Height);

            try
            {
                control.DrawToBitmap(
                bitmap,
                new Rectangle(
                    Point.Empty,
                    size));
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }

            return bitmap;
        }

        private void DetachCurrentControl()
        {
            if (_currentControl == null) return;

            this.Controls.Remove(_currentControl);

            _currentControl = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_currentControl != null)
                {
                    _currentControl.Dispose();
                    _currentControl = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
