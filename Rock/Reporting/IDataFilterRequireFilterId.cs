// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Linq.Expressions;
using Rock.Data;

namespace Rock.Reporting
{
    /// <summary>
    /// A data filter which requires it's filter Id.
    /// </summary>
    public interface IDataFilterRequireFilterId
    {
        /// <summary>
        /// Gets the expression with overrides.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="serviceInstance">The service instance.</param>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="selection">A formatted string representing the filter settings: FieldName, <see cref="ComparisonType">Comparison Type</see>, (optional) Comparison Value(s)</param>
        /// <param name="dataViewFilterId">The id of the data view filter.</param>
        /// <returns></returns>
        Expression GetExpression( Type entityType, IService serviceInstance, ParameterExpression parameterExpression, string selection, int dataViewFilterId );
    }
}
