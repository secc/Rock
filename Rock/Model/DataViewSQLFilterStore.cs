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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;

namespace Rock.Model
{
    [RockDomain( "Reporting" )]
    [Table( "DataViewSQLFilterStore" )]
    [DataContract]
    [HideFromReporting]
    public class DataViewSQLFilterStore
    {
        /// <summary>
        /// Gets or sets the data view filter identifier.
        /// </summary>
        /// <value>
        /// The entity identifier.
        /// </value>
        [DataMember]
        [Key]
        [DatabaseGenerated( DatabaseGeneratedOption.None )]
        public int DataviewFilterId { get; set; }
        public virtual DataViewFilter DataViewFilter { get; set; }


        /// <summary>
        /// Gets or sets the entity identifier.
        /// </summary>
        /// <value>
        /// The entity identifier.
        /// </value>
        [DataMember]
        [Key]
        [DatabaseGenerated( DatabaseGeneratedOption.None )]
        public int EntityId { get; set; }
    }

    public partial class SQLFilterStoreConfiguration : EntityTypeConfiguration<DataViewSQLFilterStore>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttendanceConfiguration"/> class.
        /// </summary>
        public SQLFilterStoreConfiguration()
        {
            this.HasKey( k => new { k.DataviewFilterId, k.EntityId } );
            this.HasRequired( a => a.DataViewFilter ).WithMany().HasForeignKey( p => p.DataviewFilterId ).WillCascadeOnDelete( true );
        }
    }
}
