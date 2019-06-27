/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
/*https://blogs.msdn.microsoft.com/fyuan/2007/03/10/convert-xaml-flow-document-to-xps-with-style-multiple-page-page-size-header-margin/*/

namespace GS.Server.Notes
{
    public class DocumentPaginatorWrapper : DocumentPaginator


    {
        private readonly Size m_PageSize;
        private readonly Size m_Margin;
        private readonly DocumentPaginator m_Paginator;
        private Typeface m_Typeface;
        
        public DocumentPaginatorWrapper(DocumentPaginator paginator, Size pageSize, Size margin)
        {
            m_PageSize = pageSize;
            m_Margin = margin;
            m_Paginator = paginator;
            m_Paginator.PageSize = new Size(m_PageSize.Width - margin.Width * 2,m_PageSize.Height - margin.Height * 2);
        }

        private Rect Move(Rect rect)
        {
            return rect.IsEmpty ? rect : new Rect(rect.Left + m_Margin.Width, rect.Top + m_Margin.Height,rect.Width, rect.Height);
        }
        
        public override DocumentPage GetPage(int pageNumber)
        {
            var page = m_Paginator.GetPage(pageNumber);
            // Create a wrapper visual for transformation and add extras
            var newpage = new ContainerVisual();
            var title = new DrawingVisual();
            using (var ctx = title.RenderOpen())
            {
                if (m_Typeface == null)
                {
                    m_Typeface = new Typeface("Arial");
                }
                var text = new FormattedText("GS Server Notes - Page " + (pageNumber + 1),
                    System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    m_Typeface, 14, Brushes.Black);
                ctx.DrawText(text, new Point(0, -96 / 4.0)); // 1/4 inch above page content
            }
            var background = new DrawingVisual();
            using (var ctx = background.RenderOpen())
            {
                ctx.DrawRectangle(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, page.ContentBox);
            }
            newpage.Children.Add(background); // Scale down page and center
            var smallerPage = new ContainerVisual();
            smallerPage.Children.Add(page.Visual);
            smallerPage.Transform = new MatrixTransform(0.95, 0, 0, 0.95,0.025 * page.ContentBox.Width, 0.025 * page.ContentBox.Height);
            newpage.Children.Add(smallerPage);
            newpage.Children.Add(title);
            newpage.Transform = new TranslateTransform(m_Margin.Width, m_Margin.Height);
            return new DocumentPage(newpage, m_PageSize, Move(page.BleedBox), Move(page.ContentBox));
        }

        public override bool IsPageCountValid => m_Paginator.IsPageCountValid;

        public override int PageCount => m_Paginator.PageCount;
        
        public override Size PageSize
        {
            get => m_Paginator.PageSize;
            set => m_Paginator.PageSize = value;
        }

        public override IDocumentPaginatorSource Source => m_Paginator.Source;
    }
}
