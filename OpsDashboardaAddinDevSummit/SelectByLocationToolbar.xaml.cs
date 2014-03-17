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
using ESRI.ArcGIS.Client.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using OpsDashboard = ESRI.ArcGIS.OperationsDashboard;

namespace SelectByLocation
{
  public partial class SelectByLocationToolbar : UserControl, OpsDashboard.IMapToolbar, INotifyPropertyChanged
  {
    private OpsDashboard.MapWidget MapWidget;
    private SpatialOperator _SelectedOperator;

    public IList<OpsDashboard.DataSource> TargetDataSources { get; set; }
    public FeatureLayer SourceLayer { get; set; }
    public string FirstLayerText { get; set; }
    public string MoreLayersText { get; set; }
    public SpatialOperators SpatialOperators { get; set; }
    public double Distance { get; set; }
    public List<SearchUnit> Units { get; set; }
    public SearchUnit SelectedUnit { get; set; }
    public SpatialOperator SelectedOperator
    {
      get { return _SelectedOperator; }
      set
      {
        if (_SelectedOperator != value)
        {
          _SelectedOperator = value;
          RaisePropertyChanged("SelectedOperator");
        }
      }
    }

    public SelectByLocationToolbar(OpsDashboard.MapWidget mapWidget, IList<OpsDashboard.DataSource> DataSources, FeatureLayer sourceLayer)
    {
      InitializeComponent();

      MapWidget = mapWidget;
      TargetDataSources = DataSources;
      SourceLayer = sourceLayer;

      DataContext = this;
    }

    /// <summary>
    /// OnActivated is called when the toolbar is installed into the map widget.
    /// We will set up the properties to be displayed in the UI
    /// </summary>
    public void OnActivated()
    {
      if (TargetDataSources == null || TargetDataSources.Count == 0)
        return;

      SpatialOperators = SpatialOperators.CreateSpatialOperators();
      SelectedOperator = SpatialOperators.SelectedOperator;
      Distance = 1;
      Units = GetUnits();
      SelectedUnit = Units.First(u => u.Unit == LinearUnit.SurveyMile);

      //First target data source
      FirstLayerText = TargetDataSources[0].Name;

      //If there's more than one target data source, we will show a + image and show the names of the other layers 
      //when user hover over the + image
      if (TargetDataSources.Count > 1)
      {
        var DisplayNames = from ds in TargetDataSources where ds.Name != TargetDataSources[0].Name select ds.Name;
        SetTooltipText(DisplayNames);
      }
    }

    /// <summary>
    ///  OnDeactivated is called before the toolbar is uninstalled from the map widget. 
    /// </summary>
    public void OnDeactivated()
    {
    }

    /// <summary>
    /// Execute query when Select is clicked
    /// </summary>
    private void btnSelect_Click(object sender, RoutedEventArgs e)
    {
      //From the source layer, we get the graphics selected by the op. view user 
      IEnumerable<Graphic> sourceGraphics = SourceLayer.SelectedGraphics;

      if (sourceGraphics == null || sourceGraphics.Count() == 0)
      {
        MessageBox.Show(string.Format("No selected feature(s) in the source layer \"{0}\"", SourceLayer.DisplayName),
          "Select source features",
          MessageBoxButton.OK,
          MessageBoxImage.None);
        return;
      }

      //Create a QueryHelper to run the query
      QueryHelper queryHelper = new QueryHelper(MapWidget, TargetDataSources, sourceGraphics);
      queryHelper.ExecuteQuery(SelectedOperator, Distance, SelectedUnit.Unit);
    }

    /// <summary>
    /// When the user is finished with the toolbar, revert to the default toolbar.
    /// </summary>
    private void btnDone_Click(object sender, RoutedEventArgs e)
    {
      if (MapWidget != null)
      {
        MapWidget.SetToolbar(null);
      }
    }

    #region Helper methods
    private List<SearchUnit> GetUnits()
    {
      return new List<SearchUnit>() 
            {
              new SearchUnit(LinearUnit.Centimeter),
              new SearchUnit(LinearUnit.Foot),
              new SearchUnit(LinearUnit.InternationalInch),
              new SearchUnit(LinearUnit.Kilometer),
              new SearchUnit(LinearUnit.Meter),
              new SearchUnit(LinearUnit.SurveyMile),
              new SearchUnit(LinearUnit.InternationalYard)
            };
    }

    private void SetTooltipText(IEnumerable<string> displayNames)
    {
      StringBuilder sb = new StringBuilder();
      foreach (string displayName in displayNames)
        sb.AppendLine(displayName);
      MoreLayersText = sb.ToString();
    }
    #endregion

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    void RaisePropertyChanged(string prop)
    {
      if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
    }
    #endregion

  }
}
