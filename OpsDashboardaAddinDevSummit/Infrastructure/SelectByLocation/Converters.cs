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

using System;
using System.Windows;
using System.Windows.Data;
using SelectByLocation;
using System.Collections.Generic;
using ESRI.ArcGIS.Client;
using System.Windows.Markup;
using OpsDashboard = ESRI.ArcGIS.OperationsDashboard;
using System.Linq;

namespace SelectByLocation.Converters
{
  //Return Visible if the selected operator is WithinDistanceOfSourceLayer (user needs to enter a buffer radius and a unit)
  public class OperatorToVisibilityConverter : MarkupExtension, IValueConverter
  {
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value == null)
        return Visibility.Hidden;

      SpatialOperator SelectedOperator = (SpatialOperator)value;
      return SelectedOperator.Operator == Operator.WithinDistanceOfSourceLayer ? Visibility.Visible : Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  /// <summary>
  /// Return true if count of count of SelectableLayers is > 0
  /// </summary>
  public class LayerCountToBoolConverter : MarkupExtension, IValueConverter
  {
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value == null)
        return false;

      CheckableItem<FeatureLayer>[] checkableLayers = (CheckableItem<FeatureLayer>[])value;

      //return checkableLayers.Count(item => item.IsChecked) > 0 ? true : false;
     
      return checkableLayers.Length > 0 ? true : false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  //Return Visible if there are more than one target data sources
  public class CountToVisibilityConverter : MarkupExtension, IValueConverter
  {
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value == null)
        return Visibility.Collapsed;

      IList<OpsDashboard.DataSource> dataSources = (List<OpsDashboard.DataSource>)value;
      return dataSources.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
