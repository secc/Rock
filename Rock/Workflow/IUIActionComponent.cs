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
using Rock.Data;
using Rock.Model;
using System.Web.UI.WebControls;

namespace Rock.Workflow
{
    public interface IUIActionComponent
    {
        /// <summary>
        /// Display method for UI Actions
        /// </summary>
        /// <param name="action">The workflow action.</param>
        /// <param name="rockContext">Rock Context.</param>
        /// <param name="phContent">The placeholder control where the content will be displayed.</param>
        /// <returns>Boolean indicating whether or not to actually show this.</returns>
        bool Display(WorkflowAction action, RockContext rockContext, PlaceHolder phContent);
    }
}
