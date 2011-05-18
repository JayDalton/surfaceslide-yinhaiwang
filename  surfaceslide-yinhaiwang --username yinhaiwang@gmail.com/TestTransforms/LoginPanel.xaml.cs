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
using Microsoft.Surface.Presentation.Controls;

namespace SurfaceSlide
{
    /// <summary>
    /// Interaction logic for LoginPanel.xaml
    /// </summary>
    public partial class LoginPanel: TagVisualization
    {
        /// <summary>
        /// Occurs when the user successfully validates their identity
        /// </summary>
        public event EventHandler<IdentityValidatedEventArgs> IdentityValidated;

        /// <summary>
        /// Constructor
        /// </summary>
        public LoginPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event handler for the Validate button's click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ValidateButtonClick(object sender, EventArgs e)
        {
            if (PasswordBox.Password == "1234") // Ultra Secure
            {
                OnIdentityValidated();
            }
        }

        /// <summary>
        /// Raises the IdentityValidatedEvent
        /// </summary>
        protected virtual void OnIdentityValidated()
        {
            if (IdentityValidated != null)
            {
                IdentityValidated(this, new IdentityValidatedEventArgs(Center, Orientation));
            }

            if (Visualizer.ActiveVisualizations.Contains(this))
            {
                Visualizer.RemoveVisualization(this);
            }
        }
    }
}