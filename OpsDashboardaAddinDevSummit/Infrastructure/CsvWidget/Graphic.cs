//Copyright 2014 Esri
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.​

using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Symbols;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DataHandler
{
  /// <summary>
  /// A helper class which contains graphics that are converted from features
  /// </summary>
  class Graphics : ObservableCollection<Graphic>
  {
    private static SpatialReference spRef = new SpatialReference(4326);

    public SimpleMarkerSymbol FeatureSymbol { get; set; }

    public Graphics()
    {
      Color symbolColor = Colors.Blue;
      
      FeatureSymbol = new SimpleMarkerSymbol()
      {
        Color = new SolidColorBrush(symbolColor),
        Style = SimpleMarkerSymbol.SimpleMarkerStyle.Circle,
        Size = 8
      };
    }

    /// <summary>
    /// Converts the records from a .csv file into graphics;
    /// Create a map tip for each of them to provide additional information
    /// </summary>
    public void ConvertFeaturesToGraphics(List<Feature> Features, FieldInfo LatField, FieldInfo LongField, FieldInfo DisplayField)
    {
      ClearItems();

      foreach (Feature feature in Features)
      {
        object latValue = "", longValue = "", displayValue = "";
        if (!feature.Attributes.TryGetValue(LatField.FieldName, out latValue)) continue;
        if (!feature.Attributes.TryGetValue(LongField.FieldName, out longValue)) continue;
        if (!feature.Attributes.TryGetValue(DisplayField.FieldName, out displayValue)) continue;

        double lat = 0.0, lon = 0.0;
        if (!double.TryParse((string)latValue, out lat)) continue;
        if (!double.TryParse((string)longValue, out lon)) continue;

        Graphic graphic = new Graphic()
        {
          Geometry = new MapPoint(lon, lat, spRef),
          Symbol = FeatureSymbol,
          MapTip = CreateMapTip(displayValue.ToString())
        };

        Add(graphic);
      }
    }

    private FrameworkElement CreateMapTip(string mapTipContents)
    {
      Brush mapTipBackground = Brushes.LightYellow;
      Brush mapTipBorder = Brushes.DarkGray;

      TextBlock tipText = new TextBlock()
      {
        Text = mapTipContents,
        FontSize = 20,
        FontWeight = System.Windows.FontWeights.ExtraBlack
      };

      StackPanel sp = new StackPanel()
      {
        Orientation = Orientation.Horizontal,
        Margin = new System.Windows.Thickness(5)
      };

      Border tipBorder = new Border()
      {
        BorderBrush = mapTipBorder,
        BorderThickness = new System.Windows.Thickness(2)
      };

      Grid mapTip = new Grid()
      {
        Background = mapTipBackground
      };

      sp.Children.Add(tipText);
      mapTip.Children.Add(sp);
      mapTip.Children.Add(tipBorder);

      return mapTip;
    }
  }
}
