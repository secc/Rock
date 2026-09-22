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
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb
{
    /// <summary>
    /// SECC (ROCK-9046): Shared campus-requirement logic used by the Connection Request Board
    /// and Connection Request Detail blocks. When an opportunity's connector groups are
    /// campus-scoped, a request saved without a campus can never be routed to a connector, so
    /// campus becomes required and the picker is restricted to the covered campuses.
    /// Opportunities that have a global (no campus) connector group are unaffected, which
    /// mirrors how <see cref="ConnectionRequestService"/> treats a null connector-group CampusId.
    /// </summary>
    public static class SeccConnectionCampusHelper
    {
        /// <summary>
        /// SECC (ROCK-9046): Returns the distinct campus ids covered by the opportunity's
        /// campus-scoped connector groups. Global (null campus) connector groups are excluded.
        /// </summary>
        public static List<int> GetConnectorCampusIds( RockContext rockContext, int connectionOpportunityId )
        {
            return new ConnectionOpportunityConnectorGroupService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( g => g.ConnectionOpportunityId == connectionOpportunityId && g.CampusId.HasValue )
                .Select( g => g.CampusId.Value )
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// SECC (ROCK-9046): Returns true if the opportunity has at least one global
        /// (no campus) connector group, which can serve a request for any campus.
        /// </summary>
        public static bool HasGlobalConnectorGroup( RockContext rockContext, int connectionOpportunityId )
        {
            return new ConnectionOpportunityConnectorGroupService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Any( g => g.ConnectionOpportunityId == connectionOpportunityId && !g.CampusId.HasValue );
        }

        /// <summary>
        /// SECC (ROCK-9046): Returns true if a campus is needed to route a request for the opportunity:
        /// it has at least one campus-scoped connector group and no global (no campus) one. A global
        /// group can route a request with no campus, so a mixed opportunity does not need one. Part of
        /// the approved helper surface; the two blocks reach the same answer through
        /// ApplyCampusRequirement / ValidateCampusSelection, so this is kept for callers that only need
        /// the question answered.
        /// </summary>
        public static bool IsCampusRequired( RockContext rockContext, int connectionOpportunityId )
        {
            return GetConnectorCampusIds( rockContext, connectionOpportunityId ).Any()
                && !HasGlobalConnectorGroup( rockContext, connectionOpportunityId );
        }

        /// <summary>
        /// SECC (ROCK-9046): Applies the campus requirement to an add/edit campus picker and returns
        /// whether campus is required.
        /// <para>
        /// <paramref name="currentCampusId"/> is whatever the picker currently shows - the campus an
        /// existing request was loaded with, a campus the user has since chosen, or a prefilled default.
        /// It is always kept in the item list, even when it is no longer covered, so existing data still
        /// displays; and it is only ever replaced when <paramref name="isSelectionDefault"/> says it was a
        /// default rather than a choice. A live user selection is never overwritten.
        /// </para>
        /// <para>
        /// <paramref name="isPickerAdjusted"/> records whether this method has previously changed the
        /// picker. The block persists it in ViewState so that (a) turning the setting off, or moving to
        /// an opportunity with no campus-scoped connector groups, restores the full campus list and
        /// clears Required, and (b) a picker this method never touched is left exactly as it was before
        /// ROCK-9046.
        /// </para>
        /// </summary>
        public static bool ApplyCampusRequirement(
            CampusPicker picker,
            bool settingEnabled,
            RockContext rockContext,
            int connectionOpportunityId,
            int? currentCampusId,
            int? requesterPersonId,
            ref bool isPickerAdjusted,
            ref bool isSelectionDefault )
        {
            if ( picker == null )
            {
                return false;
            }

            var connectorCampusIds = ( settingEnabled && connectionOpportunityId > 0 )
                ? GetConnectorCampusIds( rockContext, connectionOpportunityId )
                : new List<int>();

            if ( !connectorCampusIds.Any() || HasGlobalConnectorGroup( rockContext, connectionOpportunityId ) )
            {
                // Not required: the setting is off, the opportunity has no campus-scoped connector groups, or
                // it also has a global (no campus) connector group that can route a request without a campus.
                // Undo only what this method previously did - the board reuses one picker across
                // opportunities, so a narrowed list would otherwise bleed into the next opportunity.
                if ( isPickerAdjusted )
                {
                    // Required and ForceVisible are cleared before the rebuild - matching the order of the
                    // apply path below - so LoadItems recomputes stock visibility and cannot auto-select.
                    picker.Required = false;
                    picker.ForceVisible = false;
                    RestoreFullCampusList( picker );
                    picker.SelectedCampusId = currentCampusId;
                    isPickerAdjusted = false;
                    isSelectionDefault = false;
                }

                return false;
            }

            // Build the list exactly as the picker would render it, so "one campus survives" here means
            // "one campus survives there" too. The campus currently shown is always kept - and, like
            // CampusPicker.LoadItems' own selectedValue escape, is exempt from the active filter - so an
            // existing request whose campus is no longer covered or active still displays it.
            var allowedCampuses = CampusCache.All()
                .Where( c =>
                    ( currentCampusId.HasValue && c.Id == currentCampusId.Value )
                    || ( connectorCampusIds.Contains( c.Id )
                        && ( picker.IncludeInactive || !c.IsActive.HasValue || c.IsActive.Value ) ) )
                .ToList();

            // Only restrict the picker when doing so still leaves a covered campus to choose. If every
            // connector-group campus has since been deleted or deactivated, narrowing would render an
            // empty (and therefore hidden) picker that could never satisfy the requirement, so fall back
            // to the full list and let the server guard require nothing more than a non-null campus.
            var enforceCoverage = allowedCampuses.Any( c => connectorCampusIds.Contains( c.Id ) );

            // ForceVisible has to be set before the item list is rebuilt: CampusPicker.LoadItems hides the
            // picker when a single campus survives filtering, and the multi-campus branch never re-shows it.
            // It is a plain auto-property, NOT ViewState-backed, so it has to be re-set on every apply.
            picker.ForceVisible = true;

            if ( enforceCoverage )
            {
                picker.Campuses = allowedCampuses;
            }
            else
            {
                RestoreFullCampusList( picker );
            }

            // Required is set after the list is rebuilt so LoadItems cannot silently auto-select a lone
            // campus into a picker the user has not seen; the prefill below does that visibly instead.
            picker.Required = true;
            isPickerAdjusted = true;

            Func<int, bool> isServable = campusId => !enforceCoverage || connectorCampusIds.Contains( campusId );

            int? selection;

            if ( currentCampusId.HasValue && !isSelectionDefault )
            {
                // The campus on screen is the one the request was loaded with, or one the user has since
                // chosen. Either way it is not ours to replace - doing so would silently revert an edit.
                selection = currentCampusId;
            }
            else
            {
                // Only a prefilled default (or an empty picker) is (re)computed. The requester's primary
                // campus wins over a default that merely came from the block's campus filter or the
                // person preference.
                var primaryCampusId = GetPrimaryCampusId( rockContext, requesterPersonId );

                selection = primaryCampusId.HasValue && isServable( primaryCampusId.Value ) ? primaryCampusId : null;

                if ( !selection.HasValue && currentCampusId.HasValue && isServable( currentCampusId.Value ) )
                {
                    selection = currentCampusId;
                }

                if ( !selection.HasValue && enforceCoverage && allowedCampuses.Count == 1 )
                {
                    selection = allowedCampuses[0].Id;
                }

                isSelectionDefault = selection.HasValue;
            }

            picker.SelectedCampusId = selection;

            return true;
        }

        /// <summary>
        /// SECC (ROCK-9046): Server-side guard for the save handlers. Returns false (with a message) when
        /// campus is required and missing, or when the selected campus is not served by any connector
        /// group on an opportunity that has no global connector group. The campus already stored on the
        /// request is always accepted, so an existing request whose campus is no longer covered can still
        /// be saved.
        /// </summary>
        public static bool ValidateCampusSelection( RockContext rockContext, bool settingEnabled, int connectionOpportunityId, int? storedCampusId, int? selectedCampusId, out string errorMessage )
        {
            errorMessage = null;

            if ( !settingEnabled || connectionOpportunityId <= 0 )
            {
                return true;
            }

            // Mirrors the picker: nothing is required unless every connector group is campus-scoped.
            var connectorCampusIds = GetConnectorCampusIds( rockContext, connectionOpportunityId );
            if ( !connectorCampusIds.Any() || HasGlobalConnectorGroup( rockContext, connectionOpportunityId ) )
            {
                return true;
            }

            if ( !selectedCampusId.HasValue )
            {
                errorMessage = "Campus is required for this opportunity.";
                return false;
            }

            if ( storedCampusId.HasValue && selectedCampusId.Value == storedCampusId.Value )
            {
                return true;
            }

            // Mirrors the picker: only enforce coverage while a covered campus actually still exists AND is
            // active. CampusCache.All() includes inactive campuses, but the pickers on both blocks render with
            // IncludeInactive="false" - so an inactive covered campus is one the user can never choose. Counting
            // it here would enforce coverage the picker did not, and every campus the picker offers would be
            // refused (an unsaveable request).
            var enforceCoverage = CampusCache.All().Any( c => connectorCampusIds.Contains( c.Id ) && ( !c.IsActive.HasValue || c.IsActive.Value ) );

            if ( enforceCoverage && !connectorCampusIds.Contains( selectedCampusId.Value ) )
            {
                errorMessage = "The selected campus is not served by this opportunity's connector groups.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// SECC (ROCK-9046): Restores the picker's full campus list. CampusPicker exposes no way to clear
        /// its CampusIds ViewState, and assigning null would empty the list, so assign every campus -
        /// which is what the control falls back to when CampusIds was never set.
        /// </summary>
        private static void RestoreFullCampusList( CampusPicker picker )
        {
            picker.Campuses = CampusCache.All().ToList();
        }

        /// <summary>
        /// SECC (ROCK-9046): Returns the person's primary campus id, or null if unknown.
        /// </summary>
        private static int? GetPrimaryCampusId( RockContext rockContext, int? personId )
        {
            if ( !personId.HasValue )
            {
                return null;
            }

            return new PersonService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( p => p.Id == personId.Value )
                .Select( p => p.PrimaryCampusId )
                .FirstOrDefault();
        }
    }
}
