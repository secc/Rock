// <copyright>
// Copyright Southeast Christian Church
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
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb
{
    /// <summary>
    /// SECC (ROCK-9046): Shared campus-requirement logic used by the Connection Request Board
    /// and Connection Request Detail blocks. When every active connector group on an opportunity
    /// is campus-scoped, a request saved without a campus can never be routed to a connector, so
    /// campus becomes required. An opportunity that also has an active global (no campus)
    /// connector group can route a request with no campus, so it stays optional - which mirrors
    /// how <see cref="ConnectionRequestService"/> treats a null connector-group CampusId.
    /// </summary>
    public static class SeccConnectionCampusHelper
    {
        /// <summary>
        /// SECC (ROCK-9046): Returns the CampusId of every active, non-archived connector group on
        /// the opportunity. A null entry is a global connector group that serves all campuses.
        /// Inactive and archived connector groups are ignored because they cannot route anything.
        /// </summary>
        private static List<int?> GetConnectorGroupCampusIds( RockContext rockContext, int connectionOpportunityId )
        {
            return new ConnectionOpportunityConnectorGroupService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( g =>
                    g.ConnectionOpportunityId == connectionOpportunityId
                    && g.ConnectorGroup.IsActive
                    && !g.ConnectorGroup.IsArchived )
                .Select( g => g.CampusId )
                .ToList();
        }

        /// <summary>
        /// SECC (ROCK-9046): Returns true when a campus is needed to route a request for the
        /// opportunity - that is, the opportunity has at least one active connector group and
        /// every one of them is campus-scoped. An opportunity with no active connector groups,
        /// or with any active global (no campus) connector group, does not require a campus.
        /// </summary>
        public static bool IsCampusRequired( RockContext rockContext, int connectionOpportunityId )
        {
            var connectorGroupCampusIds = GetConnectorGroupCampusIds( rockContext, connectionOpportunityId );

            return connectorGroupCampusIds.Any() && connectorGroupCampusIds.All( c => c.HasValue );
        }

        /// <summary>
        /// SECC (ROCK-9046): Server-side guard for the add/edit save handlers. Returns false (with a
        /// message naming the campuses staff can pick) when campus is required and missing, or when
        /// the selected campus is not covered by any of the opportunity's connector groups.
        /// <para>
        /// Two deliberate escapes: the campus already stored on the request is always accepted, so an
        /// existing request whose campus is no longer covered can still be saved without being edited;
        /// and coverage is only enforced while at least one covered campus still exists and is active.
        /// Both pickers render with IncludeInactive="false", so if every covered campus has since been
        /// deleted or deactivated there would be no selectable campus that could pass - in that case
        /// the guard falls back to requiring nothing more than a non-null campus.
        /// </para>
        /// </summary>
        public static bool ValidateCampusSelection( RockContext rockContext, bool settingEnabled, int connectionOpportunityId, int? storedCampusId, int? selectedCampusId, out string errorMessage )
        {
            errorMessage = null;

            if ( !settingEnabled )
            {
                return true;
            }

            var connectorGroupCampusIds = GetConnectorGroupCampusIds( rockContext, connectionOpportunityId );

            if ( !connectorGroupCampusIds.Any() || !connectorGroupCampusIds.All( c => c.HasValue ) )
            {
                return true;
            }

            var coveredCampusIds = connectorGroupCampusIds.Select( c => c.Value ).Distinct().ToList();
            var servableCampusNames = GetServableCampusNames( coveredCampusIds );

            if ( !selectedCampusId.HasValue )
            {
                errorMessage = "Campus is required for this opportunity."
                    + FormatCampusList( " It is served at: {0}.", servableCampusNames );
                return false;
            }

            if ( storedCampusId.HasValue && selectedCampusId.Value == storedCampusId.Value )
            {
                // The request is being saved with the campus it was loaded with. Never block an edit
                // over a campus the person did not change.
                return true;
            }

            if ( !servableCampusNames.Any() )
            {
                // No covered campus is selectable any more, so coverage cannot be enforced without
                // making the request unsaveable. A non-null campus is all that is required.
                return true;
            }

            if ( !coveredCampusIds.Contains( selectedCampusId.Value ) )
            {
                errorMessage = "The selected campus is not served by this opportunity."
                    + FormatCampusList( " Choose one of: {0}.", servableCampusNames );
                return false;
            }

            return true;
        }

        /// <summary>
        /// SECC (ROCK-9046): Returns the names of the covered campuses that a person could actually
        /// choose - the ones that still exist and are active (a null IsActive counts as active, as it
        /// does in <see cref="Rock.Web.UI.Controls.CampusPicker"/>). Ordered the way the picker orders
        /// its items, so a message reads in the same order as the list staff are looking at.
        /// </summary>
        private static List<string> GetServableCampusNames( List<int> coveredCampusIds )
        {
            return CampusCache.All()
                .Where( c => coveredCampusIds.Contains( c.Id ) && ( !c.IsActive.HasValue || c.IsActive.Value ) )
                .OrderBy( c => c.Order )
                .Select( c => c.Name )
                .ToList();
        }

        /// <summary>
        /// SECC (ROCK-9046): Formats the campus names into the given sentence, or returns an empty
        /// string when there are none to name. Names are HTML-encoded because both blocks show the
        /// message in a NotificationBox, which renders its Text as raw HTML.
        /// </summary>
        private static string FormatCampusList( string format, List<string> campusNames )
        {
            return campusNames.Any()
                ? string.Format( format, campusNames.AsDelimited( ", ", null, true ) )
                : string.Empty;
        }
    }
}
