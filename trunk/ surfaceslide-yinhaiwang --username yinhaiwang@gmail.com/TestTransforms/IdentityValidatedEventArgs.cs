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
using System.Windows;

namespace SurfaceSlide
{
    /// <summary>
    /// Arguments for the IdentityValidated event
    /// </summary>
    public class IdentityValidatedEventArgs : EventArgs
    {
        private Point validationCenter;
        private double validationOrientation;

        /// <summary>
        /// The center of the card/tag that was just validated
        /// </summary>
        public Point ValidationCenter
        {
            get
            {
                return validationCenter;
            }
        }

        /// <summary>
        /// The orientation of the card/tag that was just validated
        /// </summary>
        public double ValidationOrientation
        {
            get
            {
                return validationOrientation;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="center">The center of the card/tag that was just validated</param>
        /// <param name="orientation">The orientation of the card/tag that was just validated</param>
        public IdentityValidatedEventArgs(Point center, double orientation)
            : base()
        {
            validationCenter = center;
            validationOrientation = orientation;
        }
    }
}
