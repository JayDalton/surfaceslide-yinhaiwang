/*
    SurfaceSlide: Digital Slide Viewer for Pathology. 
    Copyright (C) 2011 Yinhai Wang

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
    
    Contact: Yinhai Wang yinhaiwang@gmail.com
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Manipulations;

namespace SurfaceSlide
{
    /// <summary>
    /// Interaction logic for Annotation.xaml
    /// </summary>
    public partial class Annotation : SurfaceUserControl
    {
        private bool isAnnotationEnabled;
        private Shape anAnnotation;

        public Annotation(int iTypeAnnotation)
        {
            InitializeComponent();
            IsAnnotationEnabled = true;
            
            Color colour;
            Random random = new Random();

            colour = Color.FromRgb( Convert.ToByte(random.Next(255)),
                                    Convert.ToByte(random.Next(255)),
                                    Convert.ToByte(random.Next(255)));

            SolidColorBrush brush = new SolidColorBrush(colour);

            if (iTypeAnnotation == 0)
            {
                anAnnotation = new Ellipse();
            }
            else if (iTypeAnnotation == 1)
            {
                anAnnotation = new Rectangle();
            }

            anAnnotation.Stroke = brush;
            anAnnotation.StrokeThickness = 3;
            anAnnotation.Width = 300;
            anAnnotation.Height = 300;
            anAnnotation.Visibility = Visibility.Visible;

            HomerCanvas.Children.Add(anAnnotation);
            Canvas.SetLeft(anAnnotation, 0);
            Canvas.SetTop(anAnnotation, 0);

            AnnoRadioButton.Foreground = brush;
            AnnoRadioButton.Background = brush;

            AnnoText.Foreground = brush;


        }

        public bool IsAnnotationEnabled
        {
            get
            {
                return isAnnotationEnabled;
            }
            set
            {
                isAnnotationEnabled = value;
            }
        }


        public double AnnotationHeight
        {
            get
            {
                return anAnnotation.Height;
            }
            set
            {
                double valueInverse = 1 / value;

                this.Height = value;
                HomerCanvas.Height = value;

                anAnnotation.Height = value;
                anAnnotation.StrokeThickness = 900 * valueInverse;

                AnnoRadioButton.Height = 9000 * valueInverse;
                Canvas.SetTop(AnnoRadioButton, -AnnoRadioButton.Height * 0.5);

                AnnoText.Height = 45000 * valueInverse;
                AnnoText.FontSize = 6000 * valueInverse;
                Canvas.SetTop(AnnoText, value);
                AnnoText.BorderThickness = new Thickness(600 * valueInverse);
            }
        }

        public double AnnotationWidth
        {
            get
            {
                return anAnnotation.Width;
            }
            set
            {
                double valueInverse = 1 / value;

                this.Width = value;
                HomerCanvas.Width = value;
                
                anAnnotation.Width = value;
                anAnnotation.StrokeThickness = 900 * valueInverse;

                AnnoRadioButton.Width = 9000 * valueInverse;
                Canvas.SetLeft(AnnoRadioButton, (value - AnnoRadioButton.Width) * 0.5);

                AnnoText.Width = 90000 * valueInverse;
                AnnoText.FontSize = 6000 * valueInverse;
                Canvas.SetLeft(AnnoText, (value - AnnoText.Width) * 0.5);
                AnnoText.BorderThickness = new Thickness(600 * valueInverse);
            }
        }

        private void OnAnnoFinished(object sender, RoutedEventArgs e)
        {
            IsAnnotationEnabled = false;

            AnnoRadioButton.IsEnabled = false;
            AnnoRadioButton.Visibility = Visibility.Hidden;

            if (AnnoText.Text.Length == 0)
            {
                HomerCanvas.Children.Remove(AnnoText);
            }
            else
            {
//                AnnoText.IsEnabled = false;
//                AnnoText.Focusable = false;
                AnnoText.IsReadOnly = true;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                OnAnnoFinished(sender, e);
            }
        }
   }
}