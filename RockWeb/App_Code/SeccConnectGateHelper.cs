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
            if ( opportunity == null )
            {
                return false;
            }

            opportunity.LoadAttributes();
            var requiresSecurityToConnect = GetRequiresSecurityToConnect( opportunity );

            if ( !requiresSecurityToConnect.HasValue || !requiresSecurityToConnect.Value )
            {
                return true;
            }

            if ( !IsPersonAuthorizedToConnect( currentPerson, safetySecurityRoleGuid ) )
            {
                return false;
            }

            var connectableStatuses = opportunity.GetAttributeValue( "ConnectableStatuses" ).SplitDelimitedValues()
                .Select( v => v.AsIntegerOrNull() )
                .Where( v => v.HasValue )
                .ToList();

            if ( connectableStatuses.Count == 0 )
            {
                return true;
            }

            if ( connectionRequest == null )
            {
                return true;
            }

            return connectableStatuses.Contains( connectionRequest.ConnectionStatusId )
                || connectionRequest.ConnectionState == ConnectionState.Connected;
        }
    }
}
