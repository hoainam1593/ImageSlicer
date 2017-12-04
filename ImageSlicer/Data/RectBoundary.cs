using System.Drawing;

namespace ImageSlicer
{
    class RectBoundary
    {
        public Rectangle Rect { get; set; }
        public string Name { get; set; }

        public RectBoundary(Rectangle rect, string name)
        {
            Rect = rect;
            Name = name;
        }
    }
}
