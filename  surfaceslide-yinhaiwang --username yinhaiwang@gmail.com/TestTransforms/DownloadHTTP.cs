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
using System.IO;
using System.Net;

namespace SurfaceSlide
{
    class DownloadHTTP
    {
        public string dataString;
        public int dataLength;
        
        public Int32 slideHeight;
        public Int32 slideWidth;
        public Int16 slideMag;
        public Int32 slideCentreX;
        public Int32 slideCentreY;

        public DownloadHTTP()
        {
            dataLength = 0;
            dataString = null;

            slideHeight = -1;
            slideWidth = -1;
            slideCentreX = -1;
            slideCentreY = -1;
            slideMag = -1;
        }

        public DownloadHTTP(string url)
        {
            DownloadData(url);
            this.getSlideHeight();
            this.getSlideWidth();
            this.getSlideCentre();
            this.getSlideMag();
        }

        //Connects to a URL and attempts to download the file
        public void DownloadData(string url)
        {
            try
            {
                //Get a data stream from the url
                WebRequest req = WebRequest.Create(url);
                WebResponse response = req.GetResponse();
                Stream stream = response.GetResponseStream();

                //Download in chuncks
                byte[] buffer = new byte[1024];
                MemoryStream memStream = new MemoryStream();

                //Get Total Size
                dataLength = (int)response.ContentLength;

                while (true)
                {
                    //Try to read the data
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        break;
                    }
                    else
                    {
                        //Write the downloaded data
                        memStream.Write(buffer, 0, bytesRead);
                    }
                }

                dataString = System.Text.Encoding.ASCII.GetString(memStream.ToArray(), 0, dataLength); 

//                System.Diagnostics.Debug.WriteLine(dataString);

                //Clean up
                stream.Close();
                memStream.Close();
            }
            catch (Exception)
            {
                //May not be connected to the internet
                //Or the URL might not exist
                System.Windows.Forms.MessageBox.Show("There was an error accessing the URL.");
            }
        }

#region StringParcer

        public void getSlideHeight()
        {
            if ((dataLength != 0) && (dataString != null))
            {
                int startIndex = dataString.IndexOf("height=") + 7;
                int endIndex = dataString.IndexOf(",yres=");

                string heightString = dataString.Remove(0, startIndex);
                heightString = heightString.Remove(endIndex - startIndex);

                slideHeight = Convert.ToInt32(heightString);
            }
            else
            {
                slideHeight = -1;
            }
        }

        public void getSlideWidth()
        {
            if ((dataLength != 0) && (dataString != null))
            {
                int startIndex = dataString.IndexOf("width=") + 6;
                int endIndex = dataString.IndexOf(",height=");

                string widthString = dataString.Remove(0, startIndex);
                widthString = widthString.Remove(endIndex - startIndex);

                slideWidth = Convert.ToInt32(widthString);
            }
            else
            {
                slideWidth = -1;
            }
        }

        public void getSlideMag()
        {
            if ((dataLength != 0) && (dataString != null))
            {
                int startIndex = dataString.IndexOf("mag=") + 4;
                int endIndex = dataString.IndexOf(",dirList=");

                string magString = dataString.Remove(0, startIndex);
                magString = magString.Remove(endIndex - startIndex);

                slideMag = Convert.ToInt16(magString);
            }
            else
            {
                slideMag = -1;
            }
        }

        public void getSlideCentre()
        {
            slideCentreX = Convert.ToInt32(Convert.ToDouble(slideWidth) * 0.5);
            slideCentreY = Convert.ToInt32(Convert.ToDouble(slideHeight) * 0.5);
        }

#endregion

    }
}
