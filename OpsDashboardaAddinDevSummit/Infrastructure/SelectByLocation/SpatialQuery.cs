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
using ESRI.ArcGIS.Client.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpsDashboard = ESRI.ArcGIS.OperationsDashboard;

namespace SelectByLocation
{
  class QueryHelper
  {
    private GeometryService geometryService;
    private IEnumerable<Graphic> SourceFeatures;
    private IList<OpsDashboard.DataSource> TargetDataSources;
    private OpsDashboard.MapWidget MapWidget;

    public QueryHelper(OpsDashboard.MapWidget mapWidget, IList<OpsDashboard.DataSource> dataSources, IEnumerable<Graphic> sourceFeatures)
    {
      //A geometry service contains utility methods that provide access to sophisticated and frequently used geometric operations
      //such as buffer, calculate areas and lengths for geometry etc 
      geometryService = new GeometryService("http://sampleserver6.arcgisonline.com/ArcGIS/rest/services/Utilities/Geometry/GeometryServer");
      MapWidget = mapWidget;
      SourceFeatures = sourceFeatures;
      TargetDataSources = dataSources;
    }

    //Select the query method to use based on spatialOperator
    public void ExecuteQuery(SpatialOperator spatialOperator, double distance, LinearUnit unit)
    {
      //Cancel any existing async task. This can happen if user Hit the Select button multiple times 
      //before the existing operation finishes
      geometryService.CancelAsync();

      if (spatialOperator.Operator == Operator.WithinDistanceOfSourceLayer)
        Select_WithinDistanceOfSourceLayer(distance, unit);
      else if (spatialOperator.Operator == Operator.IntersectSourceLayer)
        Select_IntersectSourceLayer();
      else if (spatialOperator.Operator == Operator.CompletelyWithinSourceLayer)
        Select_CompletelyWithinSourceLayer();
      else if (spatialOperator.Operator == Operator.TouchBoundaryOfSourceLayer)
        Select_TouchBoundaryOfSourceLayer(); 
    }

    #region WithinDistanceOfSourceLayer
    //Creates buffers using the buffer distance around the source features and returns all the target features intersecting the buffer zones.
    //This method calculates a buffer, then use the intersect method to retrieve features
    private async void Select_WithinDistanceOfSourceLayer(double distance, LinearUnit unit)
    {
      #region Create buffer polygons
      //Define the params to pass to the buffer operation
      BufferParameters bufferParameters = CreateBufferParameters(MapWidget.Map.SpatialReference, distance, unit);

      //Use the service to the buffer
      BufferResult bufferResult = await geometryService.BufferTaskAsync(bufferParameters);

      if (bufferResult == null || bufferResult.Results == null || bufferResult.Results.Count == 0)
        return;

      //For each feature there will be one result
      IEnumerable<Graphic> buffers = bufferResult.Results;
      #endregion

      Select_IntersectSourceLayer(buffers);
    }

    private BufferParameters CreateBufferParameters(SpatialReference spRef, double Distance, LinearUnit Unit)
    {
      BufferParameters bufferParameters = new BufferParameters()
      {
        Unit = Unit,
        BufferSpatialReference = spRef,
        OutSpatialReference = spRef,
        UnionResults = false, //We want a separate buffer for each SourceFeatures, so UnionResults = false
      };
      bufferParameters.Distances.AddRange(new List<double> { Distance });
      bufferParameters.Features.AddRange(SourceFeatures);
      return bufferParameters;
    }
    #endregion

    #region IntersectSourceLayer
    //Query for features from the target layers that intersect with the selected feature(s) from the source layer. 
    //We use the ExecuteQueryAsync method from OpsDashboard.DataSource to do the query
    private async void Select_IntersectSourceLayer(IEnumerable<Graphic> buffers = null)
    {
      IEnumerable<Graphic> sourceGraphics = SourceFeatures;

      if (buffers != null)
        sourceGraphics = buffers;

      foreach (OpsDashboard.DataSource targetDataSource in TargetDataSources)
      {
        foreach (Graphic sourceGraphic in sourceGraphics)
        {
          //Set up the query and query result
          OpsDashboard.Query query = new OpsDashboard.Query("", sourceGraphic.Geometry, true);

          //Run the query and check the result
          OpsDashboard.QueryResult result = await targetDataSource.ExecuteQueryAsync(query);
          if ((result == null) || (result.Canceled) || (result.Features == null) || (result.Features.Count < 1))
            continue;

          // Get the array of OIDs from the query results.
          var resultOids = from resultFeature in result.Features select Convert.ToInt32(resultFeature.Attributes[targetDataSource.ObjectIdFieldName]);
          // Get the feature layer in the map for the data source.
          FeatureLayer targetLayer = MapWidget.FindFeatureLayer(targetDataSource);
          if (targetLayer == null)
            return;
          
          //Clear any features that are currently selected
          targetLayer.ClearSelection();

          // For each graphic feature in featureLayer, use its OID to find the graphic feature from the result set.
          // Note that though the featureLayer's graphic feature and the result set's feature graphic feature share the same properties,
          // they are indeed different objects
          foreach (Graphic feature in targetLayer.Graphics)
          {
            int featureOid;
            int.TryParse(feature.Attributes[targetDataSource.ObjectIdFieldName].ToString(), out featureOid);

            //If the feature is in the query result set, select it
            if ((resultOids.Contains(featureOid)))
              feature.Select();
          }
        }
      }
    }
    #endregion

    #region CompletelyWithinSourceLayer
    //This method uses the relation method to determine which target features falls within the source features
    private async void Select_CompletelyWithinSourceLayer()
    {
      await SelectUsingRelation(GeometryRelation.esriGeometryRelationIn);
    }
    #endregion

    #region TouchBoundaryOfSourceLayer
    //Query for features from the tarrget layers that are in the selected features from the source layer
    //This method uses the relation method to determine which target features touch the boundaries of the source features
    private async void Select_TouchBoundaryOfSourceLayer()
    {
      await SelectUsingRelation(GeometryRelation.esriGeometryRelationTouch);
    }
    #endregion

    #region Helper method
    /// <summary>
    /// This method purely uses the WPF SDK to query for features based on the given GeometryRelation
    /// </summary>
    private async Task SelectUsingRelation(GeometryRelation relation)
    {
      IList<Graphic> TargetFeatures = new List<Graphic>();

      foreach (OpsDashboard.DataSource targetDataSource in TargetDataSources)
      {
        FeatureLayer targetLayer = MapWidget.FindFeatureLayer(targetDataSource);
        if (targetLayer == null)
          return;

        targetLayer.ClearSelection();
        TargetFeatures = targetLayer.Graphics;

        //Both TargetFeatures and SourceFeatures are assumed to be in the same spatial reference. The relations are evaluated in 2D. 
        RelationResult result = await geometryService.RelationTaskAsync(TargetFeatures, SourceFeatures.ToList(), relation, null);

        var results = result.Results;
        if ((result == null) || (result.Results == null) || (result.Results.Count < 1))
          continue;

        //Clear any features that are currently selected
        targetLayer.ClearSelection();

        foreach (GeometryRelationPair pair in results)
        {
          //Graphic1 is the target feature, graphic 2 is the source feature
          if (pair == null)
            continue;

          Graphic actualFeature = TargetFeatures[pair.Graphic1Index];
          if (actualFeature == null)
            continue;
          else
            actualFeature.Select();
        }
      }
    }   
    #endregion
  }
}
