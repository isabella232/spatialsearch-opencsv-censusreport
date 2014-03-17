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
using ESRI.ArcGIS.OperationsDashboard;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using OpsDashboard = ESRI.ArcGIS.OperationsDashboard;

namespace SelectByLocation
{
  /// <summary>
  /// This map tool demonstrates a few ways that spatial search can be done. 
  /// 
  /// Author of an operation view specifies the target layer(s) (layers from which features will be selected)
  /// and a source layer (layer that provides feature(s) to support the spatial search
  /// - When querying using the IntersectSourceLayer operator, the target data source(s) will query for features that intesect with the source feature(s).
  /// - When querying using the WithinDistanceOfSourceLayer operator, at first GeometryService from the WPF SDK will be used to create a buffer area, 
  ///   the buffer area will then be used by the target data source(s) to query for features.
  /// - When querying using CompletelyWithinSourceLayer or TouchBoundaryOfSourceLayer, GeometryService from the WPF SDK will also be used. It calls 
  ///   RelationTaskAsync() with the target features, source features and the spatialRelation.
  /// </summary>
  [Export("ESRI.ArcGIS.OperationsDashboard.MapTool")]
  [ExportMetadata("DisplayName", "Select By Location")]
  [ExportMetadata("Description", "Select features based on the selected features of another layer")]
  [ExportMetadata("ImagePath", "/OpsDashboardaAddinDevSummit;component/Images/cursor.png")]
  [DataContract]
  public partial class SelectByLocation : UserControl, IMapTool
  {
    //All feature layers in map
    public IList<FeatureLayer> AllLayers { get; set; }

    //Layers that are selected by users
    public IList<FeatureLayer> TargetLayers { get; set; }

    //Layer that is used to get query for features from the target layers
    public FeatureLayer SourceLayer { get; set; } 

    //Urls of target layers that will be serialized and stored so that the tool's settings can be preserved
    [DataMember(Name = "TargetLayersUrls")]
    public IList<string> TargetLayersUrls { get; set; }

    //Url of source layer that will be serialized and stored so that the tool's settings can be preserved
    [DataMember(Name = "SourceLayerUrl")]
    public string SourceLayerUrl { get; set; }

    public SelectByLocation()
    {
      InitializeComponent();
    }

    #region IMapTool
    public MapWidget MapWidget { get; set; }

    public void OnActivated()
    {
    }

    public void OnDeactivated()
    {
    }

    public bool CanConfigure
    {
      get { return true; }
    }

    public bool Configure(System.Windows.Window owner)
    {
      //We don't call LoadSettings() in OnActivated because the method requires the map to be initialized
      //but the map might not be ready yet in OnActivated()
      LoadSettings();

      //To use the map tool, we only allow the layers that have selectable data sources
      IList<FeatureLayer> SelectableLayers = AllLayers.Where(layer => HasSelectableDataSource(layer)).ToList();

      //Show the Config dialog. Pass in the SelectableLayers and the TargetLayers and SourceLayer
      SelectByLocationDialog dialog = new SelectByLocationDialog(SelectableLayers, TargetLayers, SourceLayer) { Owner = owner };
      if (dialog.ShowDialog() != true)
        return false;

      //Get the new selected tagret layers and source layer from the dialog
      TargetLayers = dialog.TargetLayers;
      SourceLayer = dialog.SourceLayer;

      //Update TargetLayersUrls and SourceLayerUrl with the currently selected layers so that they can be serialized when the op. view is saved
      SaveSettings();

      return true;
    }
    #endregion

    /// <summary>
    /// Called When the map tool is clicked by an op. view user
    /// </summary>
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      //We call LoadSettings() again here because an op. view might user might run the tool without re-configuring it
      //We want to make sure we will use the saved settings to run the tool
      LoadSettings();

      if (TargetLayers == null || TargetLayers.Count == 0 || SourceLayer == null)
      {
        MessageBox.Show("Target layer and/or source layer was not set", "Layers not configured", MessageBoxButton.OK, MessageBoxImage.None);
        return;
      }

      //We need an OpsDashboard.DataSource to do query, therefore we retrieve the data sources using the TargetLayers here
      List<OpsDashboard.DataSource> TargetDataSources = GetTargetDataSources();

      MapWidget.SetToolbar(new SelectByLocationToolbar(MapWidget, TargetDataSources, SourceLayer));

      // Set the Checked property of the ToggleButton to false after work is complete.
      ToggleButton.IsChecked = false;
    }

    #region Helper methods
    /// <summary>
    /// Use the deserialized TargetLayersUrls and SourceLayerUrl to look up for the layers we are interested in
    /// </summary>
    private void LoadSettings()
    {
      //Get all feature layers from the map
      AcceleratedDisplayLayers aclyrs = MapWidget.Map.Layers.FirstOrDefault(lyr => lyr is AcceleratedDisplayLayers) as AcceleratedDisplayLayers;
      if (aclyrs == null)
        return;
      AllLayers = aclyrs.OfType<FeatureLayer>().ToList();

      //Retrieve the previously selected target layers from the stored Urls
      if (TargetLayersUrls == null)
        return;
      IList<FeatureLayer> tempTargetLayers = new List<FeatureLayer>();
      foreach (string Url in TargetLayersUrls)
        tempTargetLayers.Add(AllLayers.FirstOrDefault(lyr => lyr.Url == Url));
      TargetLayers = tempTargetLayers.Where(lyr => lyr != null).ToList();

      //Retrieve the previously selected source layer from the stored Url 
      if (string.IsNullOrEmpty(SourceLayerUrl))
        return;
      SourceLayer = AllLayers.FirstOrDefault(lyr => lyr.Url == SourceLayerUrl);
    }

    /// <summary>
    /// Update tagret layers' Urls and source layer Url so they can be be serialized and saved
    /// </summary>
    private void SaveSettings()
    {
      TargetLayersUrls = (from lyr in TargetLayers select lyr.Url).ToList();
      SourceLayerUrl = SourceLayer.Url;
    }

    /// <summary>
    /// For each input feature layer, get the first associated data source 
    /// </summary>
    public List<OpsDashboard.DataSource> GetTargetDataSources()
    {
      List<OpsDashboard.DataSource> DataSources = new List<OpsDashboard.DataSource>();
      foreach (FeatureLayer layer in TargetLayers)
      {
        OpsDashboard.DataSource dataSource = OpsDashboard.OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Name == layer.DisplayName);
        if (dataSource != null)
          DataSources.Add(dataSource);
      }
      return DataSources;
    }

    /// <summary>
    /// Check if the layer has an associated data source that is selectable
    /// </summary>
    public bool HasSelectableDataSource(FeatureLayer layer)
    {
      OpsDashboard.DataSource dataSource = OpsDashboard.OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Name.Contains(layer.DisplayName) && ds.IsSelectable);
      return dataSource == null ? false : true;
    }
    #endregion
  }
}
