using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PlistCS;

namespace ImageSlicer
{
    public partial class MainForm : Form
    {
        string m_imageFilename;
        List<RectBoundary> m_rectBoundaries = new List<RectBoundary>();
        Color m_boundaryColor = Color.Red;
        int m_maxBackgroundAlpha = 50;
        Size m_pictureSz;

        public MainForm()
        {
            InitializeComponent();
        }
        
        #region Menu handlers

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_openFileDialog.ShowDialog() == DialogResult.OK)
            {
                m_imageFilename = m_openFileDialog.FileName;

                TracePicture();
                SetPictureBackImage();
                CentralizePicture();
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ExportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_saveFileDialog.InitialDirectory = Path.GetDirectoryName(m_imageFilename);
            m_saveFileDialog.FileName = Path.GetFileNameWithoutExtension(m_imageFilename);

            if (m_saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                Plist.writeXml(BuildPListDict(), m_saveFileDialog.FileName);
            }
        }

        private void OptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new OptionsForm(m_maxBackgroundAlpha, m_boundaryColor))
            {
                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    m_maxBackgroundAlpha = form.NewAlpha;
                    m_boundaryColor = form.NewColor;

                    TracePicture();
                }
            }
        }

        #endregion

        #region Context menu handlers

        private void RenameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OnRenameContextMenu_Clicked();
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OnDeleteContextMenu_Clicked();
        }

        private void OnRenameContextMenu_Clicked()
        {
            using (var form = new RenameForm(m_treeView.SelectedNode.Text))
            {
                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    var node = m_treeView.SelectedNode;
                    var id = (int)node.Tag;

                    node.Text = form.NewName;
                    m_rectBoundaries[id].Name = form.NewName;
                }
            }
        }

        private void OnDeleteContextMenu_Clicked()
        {
            var message = "Do you want to delete '" + m_treeView.SelectedNode.Text + "'?";
            var title = "Confirm delete";
            var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                var id = (int)m_treeView.SelectedNode.Tag;
                m_rectBoundaries.RemoveAt(id);
                BuildTreeView();
            }
        }

        #endregion

        #region TreeView handlers

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag != null)
            {
                SetSourceForPicture((int)e.Node.Tag);
            }
        }

        private void TreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point p = new Point(e.X, e.Y);

                var node = m_treeView.GetNodeAt(p);
                if (node != null)
                {
                    if (node.Tag != null)
                    {
                        if ((int)node.Tag >= 0)
                        {
                            m_treeView.SelectedNode = node;
                            m_contextMenuStrip.Show(m_treeView, p);
                        }
                    }
                }
            }
        }

        private void TreeView_KeyUp(object sender, KeyEventArgs e)
        {
            var node = m_treeView.SelectedNode;
            if (node != null)
            {
                var tag = node.Tag;
                if ((tag != null) && ((int)tag >= 0))
                {
                    switch(e.KeyCode)
                    {
                        case Keys.F2:
                            OnRenameContextMenu_Clicked();
                            break;
                        case Keys.Delete:
                            OnDeleteContextMenu_Clicked();
                            break;
                    }
                }
            }
        }

        #endregion

        #region Misc handlers

        private void PictureContainer_Resize(object sender, EventArgs e)
        {
            CentralizePicture();
        }

        #endregion

        #region Helpers

        private void TracePicture()
        {
            m_rectBoundaries.Clear();

            // Get boundaries.
            var tracer = new MooreNeighborContourTracing(m_imageFilename);
            MooreNeighborContourTracing.SetMaxBackgroundAlpha(m_maxBackgroundAlpha);
            m_pictureSz = tracer.GetPictureSize();
            var bounds = tracer.Trace();

            // Calculate rect boundaries.
            int count = 0;
            foreach (var list in bounds)
            {
                var rectBound = CalculateRect(list);
                m_rectBoundaries.Add(new RectBoundary(rectBound, "Boundary " + (count++)));
            }

            // Add to tree view.
            BuildTreeView();
        }

        private static Rectangle CalculateRect(List<Point> points)
        {
            var p = points[0];
            int minX = p.X, minY = p.Y, maxX = p.X, maxY = p.Y;

            foreach (var point in points)
            {
                if (point.X < minX)
                {
                    minX = point.X;
                }
                if (point.X > maxX)
                {
                    maxX = point.X;
                }
                if (point.Y < minY)
                {
                    minY = point.Y;
                }
                if (point.Y > maxY)
                {
                    maxY = point.Y;
                }
            }

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        private static void DrawRect(Rectangle rect, Bitmap bm, Color color)
        {
            for (int x = rect.X; x <= rect.X + rect.Width; x++)
            {
                bm.SetPixel(x, rect.Y, color);
                bm.SetPixel(x, rect.Y + rect.Height, color);
            }

            for (int y = rect.Y; y <= rect.Y + rect.Height; y++)
            {
                bm.SetPixel(rect.X, y, color);
                bm.SetPixel(rect.X + rect.Width, y, color);
            }
        }

        private void SetSourceForPicture(int rectId)
        {
            // Open image.
            var image = Image.FromFile(m_imageFilename, true);
            var bitmap = (Bitmap)image;

            // Draw boundaries.
            if (rectId >= 0)
            {
                DrawRect(m_rectBoundaries[rectId].Rect, bitmap, m_boundaryColor);
            }
            else
            {
                foreach (var rect in m_rectBoundaries)
                {
                    DrawRect(rect.Rect, bitmap, m_boundaryColor);
                }
            }

            // Set image source for picture box.
            m_pictureBox.Image = image;
            m_pictureBox.Size = image.Size;
        }

        private void SetPictureBackImage()
        {
            var color1 = Color.White;
            var color2 = Color.FromArgb(191, 191, 191);
            var borderColor = Color.FromArgb(120, 120, 120);
            var squareSz = 10;

            var bitmap = new Bitmap(m_pictureSz.Width, m_pictureSz.Height);
            for (int h = 0; h < m_pictureSz.Height; h++)
            {
                for (int w = 0; w < m_pictureSz.Width; w++)
                {
                    var i = h / squareSz + w / squareSz;
                    var color = ((i % 2) == 0) ? color1 : color2;

                    if (
                        (h == 0) || (h == m_pictureSz.Height - 1) || 
                        (w == 0) || (w == m_pictureSz.Width - 1))
                    {
                        color = borderColor;
                    }

                    bitmap.SetPixel(w, h, color);
                }
            }

            m_pictureBox.BackgroundImage = bitmap;
        }

        private void CentralizePicture()
        {
            var parentSz = m_pictureBox.Parent.ClientSize;
            m_pictureBox.Location = 
                new Point(
                    parentSz.Width / 2 - m_pictureSz.Width / 2, 
                    parentSz.Height / 2 - m_pictureSz.Height / 2);
        }

        private void BuildTreeView()
        {
            int count = 0;
            var rootNode = new TreeNode(m_rectBoundaries.Count + " boundaries");

            m_treeView.Nodes.Clear();

            foreach (var rect in m_rectBoundaries)
            {
                var node = new TreeNode(rect.Name);

                node.Nodes.Add("X = " + rect.Rect.X);
                node.Nodes.Add("Y = " + rect.Rect.Y);
                node.Nodes.Add("W = " + rect.Rect.Width);
                node.Nodes.Add("H = " + rect.Rect.Height);

                node.Tag = (count++);
                rootNode.Nodes.Add(node);
            }

            rootNode.Tag = -1;
            m_treeView.Nodes.Add(rootNode);
            m_treeView.SelectedNode = rootNode;
        }

        #endregion

        #region Build plist dictionary
        
        private Dictionary<string, object> BuildPListDict()
        {
            return new Dictionary<string, object>
            {
                { "frames", BuildFramesDict() },
                { "metadata", BuildMetadataDict() }
            };
        }

        private Dictionary<string, object> BuildFramesDict()
        {
            var resultDict = new Dictionary<string, object>();

            foreach (var rect in m_rectBoundaries)
            {
                resultDict.Add(rect.Name, BuildFrameDict(rect.Rect));
            }

            return resultDict;
        }

        private Dictionary<string, object> BuildFrameDict(Rectangle rect)
        {
            return new Dictionary<string, object>
            {
                { "aliases", new List<object>() },
                { "spriteOffset", "{0,0}" },
                { "spriteSize", string.Format("{{{0},{1}}}", rect.Width, rect.Height) },
                { "spriteSourceSize", string.Format("{{{0},{1}}}", rect.Width, rect.Height) },
                { "textureRect", string.Format("{{{{{0},{1}}},{{{2},{3}}}}}", rect.X, rect.Y, rect.Width, rect.Height) },
                { "textureRotated", false }
            };
        }

        private Dictionary<string, object> BuildMetadataDict()
        {
            var filename = Path.GetFileName(m_imageFilename);

            return new Dictionary<string, object>
            {
                { "format", 3 },
                { "realTextureFileName", filename },
                { "size", string.Format("{{{0},{1}}}", m_pictureSz.Width, m_pictureSz.Height) },
                { "textureFileName", filename }
            };
        }

        #endregion
        
    }
}
