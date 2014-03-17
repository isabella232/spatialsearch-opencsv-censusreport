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

using System.Collections.Generic;

namespace SelectByLocation
{
  /// <summary>
  /// Helper class which provide the list of available spatial search methods and the selected method
  /// </summary>
  public class SpatialOperators
  {
    public IList<SpatialOperator> AvailableOperators { get; private set; }
    public SpatialOperator SelectedOperator { get; set; }

    public static SpatialOperators CreateSpatialOperators()
    {
      IList<SpatialOperator> AllOperators = new List<SpatialOperator>{
            new SpatialOperator(Operator.IntersectSourceLayer), 
            new SpatialOperator(Operator.WithinDistanceOfSourceLayer), 
            new SpatialOperator(Operator.CompletelyWithinSourceLayer),
            new SpatialOperator(Operator.TouchBoundaryOfSourceLayer)
            };

      return new SpatialOperators()
      {
        AvailableOperators = AllOperators,
        SelectedOperator = AllOperators[0]
      };
    }
  }

  /// <summary>
  /// Enum used by SpatialOperator
  /// </summary>
  public enum Operator
  {
    IntersectSourceLayer,
    WithinDistanceOfSourceLayer,
    CompletelyWithinSourceLayer,
    TouchBoundaryOfSourceLayer
  }

  /// <summary>
  /// Helper class which defines a list of available spetial search methods and their display texts
  /// </summary>
  public class SpatialOperator
  {
    public string DisplayText { get; set; }
    public Operator Operator { get; set; }

    public SpatialOperator(Operator op)
    {
      this.Operator = op;

      switch (this.Operator)
      {
        case Operator.IntersectSourceLayer:
          DisplayText = "Target features intersect the source features";
          break;
        case Operator.WithinDistanceOfSourceLayer:
          DisplayText = "Target features are within a distance of the source features";
          break;
        case Operator.CompletelyWithinSourceLayer:
          DisplayText = "Target features are within the source features";
          break;
        case Operator.TouchBoundaryOfSourceLayer:
          DisplayText = "Target features touch the boundaries of the source features";
          break;
      }
    }
  }
}
