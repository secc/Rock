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
using System.Linq;

using Rock;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb
{
    /// <summary>
    /// SECC (ROCK-8640): Shared Safety &amp; Security connect-gate logic used by the
    /// Connection Request Board and Connection Request Detail blocks. Opportunities
    /// flagged as requiring security to connect may only be connected by Rock
    /// Administrators or members of the block-configured Safety &amp; Security role.
    /// </summary>
    public static class SeccConnectGateHelper
    {
        /// <summary>
        /// Attribute keys for the SecurityToConnect flag across all known opportunity types.
        /// </summary>
        private static readonly string[] SecurityToConnectAttributeKeys =
        {
            "SecurityToConnect",
            "RequireSafetySecuritytoConnect",      // RISE
            "RequireSafetyandSecuritytoConnect"    // Lightning Lane
        };

        /// <summary>
        /// Returns the first non-null SecurityToConnect value found across all known attribute keys.
        /// Assumes the opportunity's attributes have already been loaded.
        /// </summary>
        public static bool? GetRequiresSecurityToConnect( ConnectionOpportunity opportunity )
        {
            foreach ( var key in SecurityToConnectAttributeKeys )
            {
                var value = opportunity.GetAttributeValue( key ).AsBooleanOrNull();
                if ( value.HasValue )
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if the person satisfies the S&amp;S connect gate:
        /// Rock Administrators always pass; otherwise the person must be in the configured
        /// Safety &amp; Security role. If no role is configured, only Rock Administrators pass.
        /// </summary>
        public static bool IsPersonAuthorizedToConnect( Person currentPerson, Guid? safetySecurityRoleGuid )
        {
            if ( currentPerson == null )
            {
                return false;
            }

            var adminRole = RoleCache.Get( Rock.SystemGuid.Group.GROUP_ADMINISTRATORS.AsGuid() );
            if ( adminRole != null && adminRole.IsPersonInRole( currentPerson.Guid ) )
            {
                return true;
            }

            if ( !safetySecurityRoleGuid.HasValue )
            {
                return false;
            }

            var ssRole = RoleCache.Get( safetySecurityRoleGuid.Value );
            return ssRole != null && ssRole.IsPersonInRole( currentPerson.Guid );
        }

        /// <summary>
        /// Returns true if the person may connect the request, based on the opportunity's
        /// SecurityToConnect flag, the configured Safety &amp; Security role, and the
        /// opportunity's ConnectableStatuses.
        /// Fails closed: returns false if the opportunity cannot be resolved.
        /// A null request is allowed (board add mode) so modal rendering isn't blocked.
        /// </summary>
        public static bool CanConnect( ConnectionRequest connectionRequest, ConnectionOpportunity opportunity, Person currentPerson, Guid? safetySecurityRoleGuid )
        {
            return CanConnect( connectionRequest?.ConnectionStatusId, connectionRequest?.ConnectionState, opportunity, currentPerson, safetySecurityRoleGuid );
        }

        /// <summary>
        /// Same gate, evaluated for an arbitrary status instead of a request. Lets callers ask
        /// "could the person connect a request at this status?" (e.g. the board card action menu,
        /// which must decide per status column). A null status means there is no request in context,
        /// which is allowed (board add mode) so modal rendering isn't blocked.
        /// </summary>
        public static bool CanConnect( int? connectionStatusId, ConnectionState? connectionState, ConnectionOpportunity opportunity, Person currentPerson, Guid? safetySecurityRoleGuid )
        {
            List<int> connectableStatuses;
            if ( !TryGetStatusRestriction( opportunity, currentPerson, safetySecurityRoleGuid, out connectableStatuses ) )
            {
                return false;
            }

            if ( connectableStatuses == null || !connectionStatusId.HasValue )
            {
                return true;
            }

            return connectableStatuses.Contains( connectionStatusId.Value )
                || connectionState == ConnectionState.Connected;
        }

        /// <summary>
        /// Returns the subset of <paramref name="connectionStatusIds"/> at which the person could connect a
        /// (not yet connected) request on the opportunity. Evaluates the opportunity-level half of the gate
        /// once instead of once per status, which is what the board needs when it builds its card action menu.
        /// Fails closed: returns an empty list if the opportunity cannot be resolved.
        /// </summary>
        public static List<int> GetConnectableStatusIds( IEnumerable<int> connectionStatusIds, ConnectionOpportunity opportunity, Person currentPerson, Guid? safetySecurityRoleGuid )
        {
            var statusIds = new List<int>();

            if ( connectionStatusIds == null )
            {
                return statusIds;
            }

            List<int> connectableStatuses;
            if ( !TryGetStatusRestriction( opportunity, currentPerson, safetySecurityRoleGuid, out connectableStatuses ) )
            {
                return statusIds;
            }

            statusIds.AddRange( connectableStatuses == null
                ? connectionStatusIds
                : connectionStatusIds.Where( connectableStatuses.Contains ) );

            return statusIds;
        }

        /// <summary>
        /// Evaluates the status-independent half of the gate. Returns false when the person may not connect
        /// anything on the opportunity. Returns true otherwise, with <paramref name="connectableStatuses"/>
        /// set to the status ids the opportunity restricts connecting to, or null when there is no restriction.
        /// </summary>
        private static bool TryGetStatusRestriction( ConnectionOpportunity opportunity, Person currentPerson, Guid? safetySecurityRoleGuid, out List<int> connectableStatuses )
        {
            connectableStatuses = null;

            if ( opportunity == null )
            {
                return false;
            }

            if ( opportunity.Attributes == null )
            {
                opportunity.LoadAttributes();
            }

            var requiresSecurityToConnect = GetRequiresSecurityToConnect( opportunity );

            if ( !requiresSecurityToConnect.HasValue || !requiresSecurityToConnect.Value )
            {
                return true;
            }

            if ( !IsPersonAuthorizedToConnect( currentPerson, safetySecurityRoleGuid ) )
            {
                return false;
            }

            var statuses = opportunity.GetAttributeValue( "ConnectableStatuses" ).SplitDelimitedValues()
                .Select( v => v.AsIntegerOrNull() )
                .Where( v => v.HasValue )
                .Select( v => v.Value )
                .ToList();

            if ( statuses.Count > 0 )
            {
                connectableStatuses = statuses;
            }

            return true;
        }
    }
}
