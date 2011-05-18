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
using System.Diagnostics;
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
using System.Xml;

namespace SurfaceSlide
{
    /// <summary>
    /// Interaction logic for PathXL.xaml
    /// </summary>
    public partial class PathXL : SurfaceUserControl
    {
        private const int MONITOR_WIDTH = 1024;
        private const int MONITOR_HEIGHT = 768;
        private const int MONITOR_WIDTH_2 = 512;
        private const int MONITOR_HEIGHT_2 = 384;
        private const int MONITOR_GRID_ROW = 3;
        private const int MONITOR_GRID_COL = 4;
        
        private const float ELASTIC_MARGIN = 50;
        
        private const int CACHE_SIZE = 1024;
        private const double MAX_SCALE = 1.5;

        private const int GRID_PIXEL = 256;         //-- width and height of a grid, in pixels
        private const int GRID_ROW = 9;             //-- to create a 9*12 grid, which is a lot larger than the screen size.
        private const int GRID_COL = 12;
        private const int GRID_ROW_START = 3;       //-- index of where the surface window starts among all the grids.
        private const int GRID_COL_START = 4;


        //____________________________________________________
        //  Variables related to the 2 reference point for calculating
        //      new translation and scaling
        private Point REF_POINT_A = new Point(MONITOR_WIDTH, MONITOR_HEIGHT);
        private Point REF_CENTRE  = new Point(1536, 1152);
        private Point REF_POINT_B = new Point(2048, 1536);
        private const int REF_LENGTH = 1280;

        private const bool VISIBLE = true;
        private const bool INVISIBLE = false;

        private int[] OFFSET_WIDTH = { -1536, -1280, -1024, -768, -512, -256, 0, 256, 512, 768, 1024, 1280 };
        private int[] OFFSET_HEIGHT = { -1152, -896, -640, -384, -128, 128, 384, 640, 896 };

        private int[,] RANK = { { 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4 }, 
                                { 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4 },
                                { 4, 3, 2, 1, 1, 1, 1, 1, 1, 2, 3, 4 },
                                { 4, 3, 2, 1, 0, 0, 0, 0, 1, 2, 3, 4 },
                                { 4, 3, 2, 1, 0, 0, 0, 0, 1, 2, 3, 4 },
                                { 4, 3, 2, 1, 0, 0, 0, 0, 1, 2, 3, 4 },
                                { 4, 3, 2, 1, 1, 1, 1, 1, 1, 2, 3, 4 },
                                { 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4 },
                                { 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4 } };

        private bool[,] SHARP_FLAG = { { false, false, false, false, false, false, false, false, false, false, false, false }, 
                                       { false, false, false, false, false, false, false, false, false, false, false, false }, 
                                       { false, false, false, false, false, false, false, false, false, false, false, false }, 
                                       { false, false, false, false, true , true , true , true , false, false, false, false }, 
                                       { false, false, false, false, true , true , true , true , false, false, false, false }, 
                                       { false, false, false, false, true , true , true , true , false, false, false, false }, 
                                       { false, false, false, false, false, false, false, false, false, false, false, false }, 
                                       { false, false, false, false, false, false, false, false, false, false, false, false }, 
                                       { false, false, false, false, false, false, false, false, false, false, false, false } };

        private double[] RANK_GRID_PIXEL_CACHE = { GRID_PIXEL, 64, 64, 32, 16 };
        private double[] RANK_GRID_PIXEL_CACHE_TIMES = { 1, 4, 4, 8, 16 };
        private double[] RANK_GRID_PIXEL_CACHE_PERCENT = { 1, 0.25, 0.25, 0.125, 0.0625 };

        private Int32 currentGridIndex;
        private Point referencePointA;      //-- Top left corner
        private Point referencePointB;      //-- Bottom Right corner

        private Matrix virtualSlideMatrix = Matrix.Identity;
        private Affine2DManipulationProcessor manipulationProcessor;
        
        //__________________________________________________________________
        //  The absolute location of the centre of the virtual slide at 40X
        private Int32 iVirtualSlideCentreX;           //-- Horizontal Offset
        private Int32 iVirtualSlideCentreY;           //-- Vertical offset

        //__________________________________________________________________
        //  The current resolution of the virtual slide in the range of [0, 1]
        private double dVirtualSlideResolution; 
//        private Int32 iVirtualSlidePlane;     //-- which plane for 3D virtual slides

        private double dMaxMag;

        DownloadHTTP pathXLInfo;
        string slideNameString;

//
        private Dictionary<int, Annotation> AnnotationDictionary;
        private int circleDictionaryCounter;
        private bool isAnnotationActive;

        private const int DRAG_THRESHOLD = 15;

        public PathXL()
        {
            InitializeComponent();
        }

        private void InitializePathXL()
        {
            //______________________________________________________
            //  Collection of all virutal slides
            //  TODO: to users, please contact iPath Diagnostics for licensing regarding slide hosting and server interface API.
            pathXLInfo = "";

            double xx = Convert.ToDouble(MONITOR_HEIGHT) / Convert.ToDouble(pathXLInfo.slideHeight);
            double yy = Convert.ToDouble(MONITOR_WIDTH) / Convert.ToDouble(pathXLInfo.slideWidth);

            dVirtualSlideResolution = Math.Max(xx, yy);
            dMaxMag = pathXLInfo.slideMag;

            iVirtualSlideCentreX = Convert.ToInt32(MONITOR_WIDTH_2 / dVirtualSlideResolution);
            iVirtualSlideCentreY = Convert.ToInt32(MONITOR_HEIGHT_2 / dVirtualSlideResolution);

            InitializeManipulationProcessor();

            currentGridIndex = 0;
            referencePointA = new Point(0, 0);
            referencePointB = new Point(0, 0);

            AnnotationDictionary = new Dictionary<int, Annotation>();
            circleDictionaryCounter = 0;
            isAnnotationActive = false;

            ResetGrid();
        }

        private void ResetGrid()
        {
            VirtualSlideGrid.Children.RemoveRange(1, currentGridIndex);

            virtualSlideMatrix = Matrix.Identity;
            VirtualSlideGrid.RenderTransform = new MatrixTransform(virtualSlideMatrix);

            MenuScatterItem.IsActive = false;
            ScatterCanvas.IsActive = true;

            foreach (KeyValuePair<int, Annotation> circleItem in AnnotationDictionary)
            {
                /*
                                Console.WriteLine( "Customer ID: {0}, Name: {1}",
                                                   circleItem.Key,
                                                   circleItem.Value.IsAnnotationEnabled);
                */
                PathXLCanvas.Children.Remove(circleItem.Value);
            }

            AnnotationDictionary.Clear();

            string buffer;

            buffer = "Virtual slide size = (" + pathXLInfo.slideWidth.ToString("F00") +
                                         " ," + pathXLInfo.slideHeight.ToString("F00") + ")\n" +
                     "Maximum Magnification = " + dMaxMag.ToString("F01") + "X";

            InfoBar.Content = buffer;
        }

        private void InitializeManipulationProcessor()
        {
            Affine2DManipulations supportedManipulation = Affine2DManipulations.TranslateX |
                                                          Affine2DManipulations.TranslateY |
                                                          Affine2DManipulations.Scale |
                                                          Affine2DManipulations.Rotate;
          
            //______________________________________________________
            //  It has to be PathXLCanvas rather than VirtualSlideGrid
            manipulationProcessor = new Affine2DManipulationProcessor(supportedManipulation, PathXLCanvas);

            manipulationProcessor.Affine2DManipulationStarted += 
                new EventHandler<Affine2DOperationStartedEventArgs>(OnAffine2DManipulationStarted);
            manipulationProcessor.Affine2DManipulationCompleted += 
                new EventHandler<Affine2DOperationCompletedEventArgs>(OnAffine2DManipulationCompleted);
            manipulationProcessor.Affine2DManipulationDelta += 
                new EventHandler<Affine2DOperationDeltaEventArgs>(OnAffine2DManipulationDelta_MatrixOperation);
        }

#region PanScale
        private void Pan(double dPanX, double dPanY)
        {
            if (dVirtualSlideResolution == 0)
            {
                return;
            }

            iVirtualSlideCentreX -= Convert.ToInt32(dPanX / dVirtualSlideResolution);          //-- Column
            iVirtualSlideCentreY -= Convert.ToInt32(dPanY / dVirtualSlideResolution);          //-- Row

            if (iVirtualSlideCentreX + MONITOR_WIDTH >= pathXLInfo.slideWidth)
            {
                iVirtualSlideCentreX = pathXLInfo.slideWidth - MONITOR_WIDTH;
            }
            if (iVirtualSlideCentreX <= 0)
            {
                iVirtualSlideCentreX = 0;
            }
            if (iVirtualSlideCentreY + MONITOR_HEIGHT >= pathXLInfo.slideHeight)
            {
                iVirtualSlideCentreY = pathXLInfo.slideHeight - MONITOR_HEIGHT;
            }
            if (iVirtualSlideCentreY <= 0)
            {
                iVirtualSlideCentreY = 0;
            }

            //______________________________________________________
            //  Load 12 grid images in "direction order"
            LoadGrid(dVirtualSlideResolution);
        }

        private Point getTranslationFromScaleAt(double currentResolution)
        {
            Point translation40X = new Point();
            
            Point postTranslation = getTranslation(referencePointA, referencePointB);

            translation40X.X = postTranslation.X / currentResolution;
            translation40X.Y = postTranslation.Y / currentResolution;

            return translation40X;
        }

        private void Scale(double dScale)
        {
            dVirtualSlideResolution *= dScale;

            
            //_______________________________________________
            //  This part of the code is to deal with scaleAt situation
            Point translation40X = new Point();

            translation40X = getTranslationFromScaleAt(dVirtualSlideResolution);

            iVirtualSlideCentreX -= Convert.ToInt32(translation40X.X);
            iVirtualSlideCentreY -= Convert.ToInt32(translation40X.Y);
            //.end


            if (dVirtualSlideResolution > MAX_SCALE)
            {
                dVirtualSlideResolution = MAX_SCALE;
            }

            LoadGrid(dVirtualSlideResolution);
        }

        private void LoadGrid(double dVirtualSlideResolution)
        {
            int i, j;
            string buffer;

            //_________________________________________________
            //
            //  I. First of all, set all grids which are visible at the surface table
            //      which is a 1024 * 768 area, representing 12 grids.
            //_________________________________________________
            //
            for (i = GRID_ROW_START; i < GRID_ROW_START + MONITOR_GRID_ROW; i++)
            {
                for (j = GRID_COL_START; j < GRID_COL_START + MONITOR_GRID_COL; j++)
                {
                    setOneGrid(i, j, dVirtualSlideResolution, VISIBLE);
                }
            }

            //_________________________________________________
            //  Setup the message bar
            buffer = "Current Location = (" + (iVirtualSlideCentreX * dVirtualSlideResolution).ToString("F00") +
                                       " ," + (iVirtualSlideCentreY * dVirtualSlideResolution).ToString("F00") + ")" +
                     "\nCurrent Virtual slide size = (" + (pathXLInfo.slideWidth * dVirtualSlideResolution).ToString("F00") +
                                                    " ," + (pathXLInfo.slideHeight * dVirtualSlideResolution).ToString("F00") + ")" +
                     "\n@ Magnification = " + (dVirtualSlideResolution * dMaxMag).ToString("F01") + "X";

            InfoFixed.Text = buffer;

            //_________________________________________________
            //
            //  II. Afterwards, set all the invisible grids
            //      when using a 12 * 9 grid setting, there are 96 another grid
            //      to be setup.
            //_________________________________________________
            //
            for (i = 0; i < GRID_ROW; i++)
            {
                for (j = 0; j < GRID_COL; j++)
                {
                    if (((i >= GRID_ROW_START) &&
                         (i < GRID_ROW_START + MONITOR_GRID_ROW)) &&
                        ((j >= GRID_COL_START) &&
                         (j < GRID_COL_START + MONITOR_GRID_COL)))
                    {
                        continue;
                    }
                    else
                    {
                        setOneGrid(i, j, dVirtualSlideResolution, INVISIBLE);
                    }
                }
            }
        }

        //__________________________________________________________
        //  i is column index
        //  j is row index
        private void setOneGrid(int i, int j, double dVirtualSlideResolution, bool bVisible)
        {
            string imageURL;

            Image tempImage = new Image();
            BitmapImage bi = new BitmapImage();

            //_________________________________________________
            //  This is a funny one, I still don't have a clue why should I minus
            //      half width and half height.
            double xValue = iVirtualSlideCentreX * dVirtualSlideResolution + OFFSET_WIDTH[j];
            double yValue = iVirtualSlideCentreY * dVirtualSlideResolution + OFFSET_HEIGHT[i];

            //_________________________________________________
            //  make a judgement of if the slide is panning/zooming out of the minimum range
            //  If yes, I should possibly do a padding??
            if ((xValue < 0) ||
                (yValue < 0) ||
                (xValue > pathXLInfo.slideWidth * dVirtualSlideResolution) ||
                (yValue > pathXLInfo.slideHeight * dVirtualSlideResolution))
            {
                imageURL = @".\Resources\white.jpg";
            }
            else
            {
                if (bVisible == VISIBLE)
                {
                    imageURL = "";
                    //  TODO: to users, please contact iPath Diagnostics for licensing regarding slide hosting and server interface API.
                }
                else
                {
                    imageURL = "";
                    //  TODO: to users, please contact iPath Diagnostics for licensing regarding slide hosting and server interface API.     
                }
            }

            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnDemand;
            bi.UriSource = new Uri(imageURL, UriKind.RelativeOrAbsolute);
            bi.EndInit();
          
            tempImage.Source = bi;

            Grid.SetRow(tempImage, i);
            Grid.SetColumn(tempImage, j);

            VirtualSlideGrid.Children.Add(tempImage);
            currentGridIndex = VirtualSlideGrid.Children.IndexOf(tempImage);

            bi = null;
            tempImage = null;
        }
#endregion

#region ManipulationDelta
        /// <summary>
        /// Handles changes in manipulation.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="e">Event argument</param>
        private void OnAffine2DManipulationStarted(object sender, Affine2DOperationStartedEventArgs e)
        {
            MenuScatterItem.IsActive = false;

            if (circleDictionaryCounter != 0)
            {
                if (AnnotationDictionary[circleDictionaryCounter - 1].IsAnnotationEnabled == true)
                {
                    isAnnotationActive = true;
                    return;
                }
            }

            isAnnotationActive = false;
        }
       
        private void OnAffine2DManipulationDelta_MatrixOperation(object sender, Affine2DOperationDeltaEventArgs e)
        {
            if (isAnnotationActive == true)
            {
                deltaAnnotation(e, AnnotationDictionary[circleDictionaryCounter-1]);
            }
            else
            {
                foreach (KeyValuePair<int, Annotation> circleItem in AnnotationDictionary)
                {
                    deltaAnnotation(e, circleItem.Value);
                }
                deltaGrid(e);
            }
        }

        private void OnAffine2DManipulationCompleted(object sender, Affine2DOperationCompletedEventArgs e)
        {
            if (isAnnotationActive == true)
            {
                return;
            }
    
            //_________________________________________________
            //
            double postScale = getScale(referencePointA, referencePointB);
            Point postTranslation = getTranslation(referencePointA, referencePointB);

            //_________________________________________________
            //  if only translation happened
            if (e.TotalScale == 1.0)
            {
                //_____________________________________________
                // Deblur 
                if ((Math.Abs(postTranslation.X) <= MONITOR_WIDTH) && (Math.Abs(postTranslation.Y) <= MONITOR_HEIGHT))
                {
                    int i;
                    int j;

                    for (i = 0; i < GRID_ROW; i++)
                    {
                        for (j = 0; j < GRID_COL; j++)
                        {
                            if (((i >= GRID_ROW_START) &&
                                 (i < GRID_ROW_START + MONITOR_GRID_ROW)) &&
                                ((j >= GRID_COL_START) &&
                                 (j < GRID_COL_START + MONITOR_GRID_COL)))
                            {
                                continue;
                            }
                            else
                            {
                                double xLocation = 256 * j + postTranslation.X;
                                double yLocation = 256 * i + postTranslation.Y;

                                if (((xLocation + 256 >= 1024) &&
                                     (xLocation       <= 2048)) &&
                                    ((yLocation + 256 >= 768) &&
                                     (yLocation       <= 1536)))
                                {
                                    if (SHARP_FLAG[i, j] == false)
                                    {
                                        setOneGrid(i, j, dVirtualSlideResolution, VISIBLE);
                                    }
                                }
                            }
                        }
                    }
                }
                //_____________________________________________
                // Reload
                else 
                {
                    ResetSharpFlag();
                    VirtualSlideGrid.Children.RemoveRange(1, currentGridIndex);
                    VirtualSlideGrid.RenderTransform = new MatrixTransform(Matrix.Identity);

                    switch (ApplicationLauncher.Orientation)
                    {
                        case UserOrientation.Top:
                            // Surface is upside-down
                            Pan(-postTranslation.X, -postTranslation.Y);
                            break;
                        case UserOrientation.Bottom:
                            // Surface has normal orientation
                            Pan(postTranslation.X, postTranslation.Y);
                            break;
                        default:
                            return;
                    }
                }
            }
            else
            {
                ResetSharpFlag();
                VirtualSlideGrid.Children.RemoveRange(1, currentGridIndex);
                VirtualSlideGrid.RenderTransform = new MatrixTransform(Matrix.Identity);
            }

            //_________________________________________________
            //
            if (postScale != 1.0)
            {
                Scale(postScale);
                return;
            }

            //_________________________________________________
            //  How could I do a refresh???
            GridViewControl.UpdateLayout();
        }
#endregion

#region ManipulationDelta Utilities
        private void deltaAnnotation(Affine2DOperationDeltaEventArgs e, Annotation aCicleObject)
        {
            MatrixTransform transformMatrix;
            Matrix aMatrix = Matrix.Identity;

            //_________________________________________________
            //  Define a matrix tranform for manipulation processor
            transformMatrix = aCicleObject.RenderTransform as MatrixTransform;

            if (transformMatrix != null)
            {
                aMatrix = transformMatrix.Matrix;
            }

            //_________________________________________________
            //  Translation
            if ((e.Delta.X != 0) || (e.Delta.Y != 0) &&
                (!double.IsInfinity(e.Delta.X)) &&
                (!double.IsInfinity(e.Delta.Y)) &&
                (!double.IsNaN(e.Delta.X)) &&
                (!double.IsNaN(e.Delta.Y)))
            {
                aMatrix.Translate(e.Delta.X, e.Delta.Y);
            }

            //_________________________________________________
            //  Scaling
            if (e.ScaleDelta != 1.0)
            {
                aMatrix.ScaleAt(e.ScaleDelta,
                                e.ScaleDelta,
                                e.ManipulationOrigin.X - MONITOR_WIDTH,
                                e.ManipulationOrigin.Y - MONITOR_HEIGHT);

                aCicleObject.AnnotationHeight *= e.ScaleDelta;
                aCicleObject.AnnotationWidth *= e.ScaleDelta;
            }

            //_________________________________________________
            //  Apply the matrix transformation
            aCicleObject.RenderTransform = new MatrixTransform(aMatrix);
        }

        private void deltaGrid(Affine2DOperationDeltaEventArgs e)
        {
            MatrixTransform transformMatrix;

            //_________________________________________________
            //  Define a matrix tranform for manipulation processor
            transformMatrix = VirtualSlideGrid.RenderTransform as MatrixTransform;

            if (transformMatrix != null)
            {
                virtualSlideMatrix = transformMatrix.Matrix;
            }

            //_________________________________________________
            //  Translation
            if ((e.Delta.X != 0) || (e.Delta.Y != 0) &&
                (!double.IsInfinity(e.Delta.X)) &&
                (!double.IsInfinity(e.Delta.Y)) &&
                (!double.IsNaN(e.Delta.X)) &&
                (!double.IsNaN(e.Delta.Y)))
            {
                virtualSlideMatrix.Translate(e.Delta.X, e.Delta.Y);
            }

            //_________________________________________________
            //  Scaling
            if (e.ScaleDelta != 1.0)
            {
                virtualSlideMatrix.ScaleAt(e.ScaleDelta, e.ScaleDelta, e.ManipulationOrigin.X, e.ManipulationOrigin.Y);
            }

            //_________________________________________________
            //  New location for the reference point
            referencePointA = VirtualSlideGrid.TranslatePoint(REF_POINT_A, PathXLCanvas);
            referencePointB = VirtualSlideGrid.TranslatePoint(REF_POINT_B, PathXLCanvas);

            //_________________________________________________
            //  Apply the matrix transformation
            VirtualSlideGrid.RenderTransform = new MatrixTransform(virtualSlideMatrix);
        }

        private Point getTranslation(Point referencePointA, Point referencePointB)
        {
            Point CentrePost = new Point();
            Point TranslationDelta = new Point();

            CentrePost.X = (referencePointB.X + referencePointA.X) * 0.5;
            CentrePost.Y = (referencePointB.Y + referencePointA.Y) * 0.5;

            TranslationDelta.X = CentrePost.X - REF_CENTRE.X;
            TranslationDelta.Y = CentrePost.Y - REF_CENTRE.Y;

            return TranslationDelta;
        }

        private double getScale(Point referencePointA, Point referencePointB)
        {
            double LengthPost = Math.Sqrt((referencePointB.X - referencePointA.X) * (referencePointB.X - referencePointA.X) +
                                          (referencePointB.Y - referencePointA.Y) * (referencePointB.Y - referencePointA.Y));

            return (LengthPost / REF_LENGTH);
        }

        private void ResetSharpFlag()
        { 
            int i, j;

            for (i = 0; i < GRID_ROW; i++)
            {
                for (j = 0; j < GRID_COL; j++)
                {
                    if (((i >= GRID_ROW_START) &&
                         (i < GRID_ROW_START + MONITOR_GRID_ROW)) &&
                        ((j >= GRID_COL_START) &&
                         (j < GRID_COL_START + MONITOR_GRID_COL)))
                    {
                        SHARP_FLAG[i, j] = true;
                    }
                    else
                    {
                        SHARP_FLAG[i, j] = false;
                    }
                }
            }
        }
#endregion

#region CaptureContact
        /// <summary>
        /// Captures contact and starts the manipulation processor.
        /// </summary>
        /// <param name="e">Contact event.</param>
        /// 
        protected override void OnContactDown(ContactEventArgs e)
        {
            base.OnContactDown(e);

            if (MenuScatterItem.IsAnyContactCaptured == true)
            {
                return;
            }
            else
            {
                MenuScatterItem.IsActive = false;
            }

            if (!e.Contact.IsFingerRecognized) 
            {
                return; 
            }

            // Capture this contact
            e.Contact.Capture(this);
            
            if (manipulationProcessor != null)
                manipulationProcessor.BeginTrack(e.Contact);

            // Mark this event as handled
            e.Handled = true;
        }

        protected override void OnContactChanged(ContactEventArgs e)
        {
            base.OnContactChanged(e);

            if (!e.Contact.IsFingerRecognized) 
            {
                return; 
            }

            if (MenuScatterItem.IsAnyContactCaptured == true)
            {
                return;
            }

            Point position = e.Contact.GetPosition(this);
            if (position.X < 0 ||
                position.Y < 0 ||
                position.X > this.ActualWidth ||
                position.Y > this.ActualHeight)
            {
                e.Contact.Capture(this, CaptureMode.None);
                e.Handled = true;
                return;
            }

            e.Contact.Capture(this, CaptureMode.SubTree);

            if (manipulationProcessor != null) 
                manipulationProcessor.BeginTrack(e.Contact);
            e.Handled = true;
        }

        protected override void OnContactUp(ContactEventArgs e)
        {
            base.OnContactUp(e);

            if (!e.Contact.IsFingerRecognized) 
            {
                return; 
            }

            if (MenuScatterItem.IsAnyContactCaptured == true)
            {
                return;
            }

            e.Contact.Capture(this, CaptureMode.None);

            if (manipulationProcessor != null)
            {
                manipulationProcessor.EndTrack(e.Contact);
            }
        }
#endregion

#region MenuClicks
        private void OnCloseSlide(object sender, RoutedEventArgs e)
        {
            slideNameString = "";

            VirtualSlideGrid.Children.RemoveRange(1, currentGridIndex);

            iVirtualSlideCentreX = 0;
            iVirtualSlideCentreY = 0;
            dVirtualSlideResolution = 0;

            dMaxMag = 0;
            ResetGrid();

            pathXLInfo = null;

            manipulationProcessor = null;

            InfoBar.Content = "";
            InfoFixed.Text = "";

            //____________________________________________________________
            //  Hide the SlideListBox
            Canvas.SetTop(SlideListBox, 377.5);
            Canvas.SetTop(MainMenu, 1756);

        }

        private void OnReset(object sender, RoutedEventArgs e)
        {
            ResetGrid();

            InitializePathXL();
            LoadGrid(dVirtualSlideResolution);
        }

        private void OnInfo(object sender, RoutedEventArgs e)
        {
            MenuScatterItem.IsActive = true;
        }

        private void OnAnnotationCircle(object sender, RoutedEventArgs e)
        {
            Annotation anAnnotation = new Annotation(0);

            anAnnotation.AnnotationHeight = 300;
            anAnnotation.AnnotationWidth = 300;

            PathXLCanvas.Children.Add(anAnnotation);
            Canvas.SetLeft(anAnnotation, MONITOR_WIDTH);
            Canvas.SetTop(anAnnotation, MONITOR_HEIGHT);

            anAnnotation.IsAnnotationEnabled = true;

            AnnotationDictionary.Add(circleDictionaryCounter++, anAnnotation);
        }

        private void OnAnnotationRectangle(object sender, RoutedEventArgs e)
        {
            Annotation anAnnotation = new Annotation(1);

            anAnnotation.AnnotationHeight = 300;
            anAnnotation.AnnotationWidth = 300;

            PathXLCanvas.Children.Add(anAnnotation);
            Canvas.SetLeft(anAnnotation, MONITOR_WIDTH);
            Canvas.SetTop(anAnnotation, MONITOR_HEIGHT);

            anAnnotation.IsAnnotationEnabled = true;

            AnnotationDictionary.Add(circleDictionaryCounter++, anAnnotation);
        }

        private void OnHideInfo(object sender, RoutedEventArgs e)
        {
            MenuScatterItem.IsActive = false;
            ScatterCanvas.IsActive = true;
        }
#endregion

#region Scoll bar preview and dragging events

        /// <summary>
        /// Attempts to get an ancestor of the passed-in element with the given type.
        /// </summary>
        /// <typeparam name="T">Type of ancestor to search for.</typeparam>
        /// <param name="descendent">Element whose ancestor to find.</param>
        /// <param name="ancestor">Returned ancestor or null if none found.</param>
        /// <returns>True if found, false otherwise.</returns>
        private static T GetVisualAncestor<T>(DependencyObject descendent) where T : class
        {
            T ancestor = null;
            DependencyObject scan = descendent;
            ancestor = null;

            while (scan != null && ((ancestor = scan as T) == null))
            {
                scan = VisualTreeHelper.GetParent(scan);
            }

            return ancestor;
        }

        private void OnPreviewContactDown(object sender, ContactEventArgs e)
        {
            InputDeviceHelper.ClearDeviceState(e.Device);
            InputDeviceHelper.InitializeDeviceState(e.Device);
        }

        private void OnPreviewContactChanged(object sender, ContactEventArgs e)
        {
            // If this is a contact whose state has been initialized when its down event happens
            if (InputDeviceHelper.GetDragSource(e.Device) != null)
            {
                StartDragDrop(SlideListBox, e);
            }
        }

        private void OnPreviewContactUp(object sender, ContactEventArgs e)
        {
            //____________________________________________
            //
            InputDeviceHelper.ClearDeviceState(e.Device);
        }

        /// <summary>
        /// Try to start Drag-and-drop for a listBox.
        /// </summary>
        /// <param name="sourceListBox"></param>
        /// <param name="e"></param>
        private void StartDragDrop(ListBox sourceListBox, InputEventArgs e)
        {
            InputDeviceHelper.InitializeDeviceState(e.Device);

            Vector draggedDelta = InputDeviceHelper.DraggedDelta(e.Device, (UIElement)sourceListBox);

            // If this input device has moved more than Threshold pixels horizontally,
            // put it to the ignore list and never try to start drag-and-drop with it.
            if (Math.Abs(draggedDelta.X) > DRAG_THRESHOLD)
            {
                return;
            }

            // If this contact has moved less than Threshold pixels vertically 
            // then this is not a drag-and-drop yet.
            if (Math.Abs(draggedDelta.Y) < DRAG_THRESHOLD)
            {
                return;
            }

            // try to start drag-and-drop,
            // verify that the cursor the contact was placed at is a ListBoxItem
            DependencyObject downSource = InputDeviceHelper.GetDragSource(e.Device);
            Debug.Assert(downSource != null);

            SurfaceListBoxItem draggedListBoxItem = GetVisualAncestor<SurfaceListBoxItem>(downSource);
            Debug.Assert(draggedListBoxItem != null);

            //____________________________________________________________
            // Get Xml source.
            XmlElement data = draggedListBoxItem.Content as XmlElement;

            //____________________________________________________________
            //  Name of the virutal slide
            string virtualSlideName = data.GetAttribute("Name");

            //  TODO: to users, please contact iPath Diagnostics for licensing regarding slide hosting and server interface API.
            //  slideNameString is the URL where a digital slide is located.
            slideNameString = "http://123.123.123.123/somelocation/" + virtualSlideName + ".svs";
           
            InitializePathXL();
            LoadGrid(dVirtualSlideResolution);

            //____________________________________________________________
            //  Hide the SlideListBox
            Canvas.SetTop(SlideListBox, 768);
            Canvas.SetTop(MainMenu, 1486);

            e.Handled = true;
        }
#endregion 

    }
}
