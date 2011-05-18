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
using System.Xml;
using System.Windows;
using System.Windows.Controls;

namespace SurfaceSlide
{
    /// <summary>
    /// Template selector class for the items in the SlideListBox.
    /// </summary>
    public class ListTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Template for the item selected as the starting item.
        /// </summary>
        public DataTemplate StartingItemTemplate { get; set; }

        /// <summary>
        /// Template for items that are not the starting item.
        /// </summary>
        public DataTemplate NormalItemTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            XmlElement data = item as XmlElement;
            return (data != null && data.GetAttribute("Name") == "CHCCase01") ? StartingItemTemplate : NormalItemTemplate;
        }
    }
}