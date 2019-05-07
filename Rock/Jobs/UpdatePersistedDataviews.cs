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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace Rock.Jobs
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Quartz.IJob" />
    [DisallowConcurrentExecution]
    [IntegerField( "SQL Command Timeout", "Maximum amount of time (in seconds) to wait for each SQL command to complete. Leave blank to use the default for this job (300 seconds). ", false, 5 * 60, "General", 1, "SqlCommandTimeout" )]
    [IntegerField( "Thread Count", "Number of concurent threads to run at a time", false, 10, "General", 1, "ThreadCount" )]
    public class UpdatePersistedDataviews : IJob
    {
        RockContext rockContext;
        ConcurrentBag<int> dataViews = new ConcurrentBag<int>();
        int updatedDataViewCount = 0;
        int sqlCommandTimeout;

        /// <summary>
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public UpdatePersistedDataviews()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            StringBuilder results = new StringBuilder();
            sqlCommandTimeout = dataMap.GetString( "SQLCommandTimeout" ).AsIntegerOrNull() ?? 300;
            rockContext = new RockContext();

            var currentDateTime = RockDateTime.Now;

            // get a list of all the dataviews that need to be refreshed
            var expiredPersistedDataViews = new DataViewService( rockContext ).Queryable()
                .Where( a => a.PersistedScheduleIntervalMinutes.HasValue )
                .Where( a =>
                  ( a.PersistedLastRefreshDateTime == null )
                  || ( System.Data.Entity.SqlServer.SqlFunctions.DateAdd( "mi", a.PersistedScheduleIntervalMinutes.Value, a.PersistedLastRefreshDateTime.Value ) < currentDateTime )
                 )
                 .Select( a => a.Id )
                 .ToList();

            foreach ( var dataView in expiredPersistedDataViews )
            {
                dataViews.Add( dataView );
            }

            List<Task> taskList = new List<Task>();

            var threadCount = dataMap.GetString( "ThreadCount" ).AsIntegerOrNull() ?? 10;
            for ( int i = 0; i < threadCount; i++ )
            {
                taskList.Add( Task.Run( () => UpdateDataViews() ) );
            }

            Task.WaitAll( taskList.ToArray() );


            results.AppendLine( $"Updated {updatedDataViewCount} {"dataview".PluralizeIf( updatedDataViewCount != 1 )}" );
            context.UpdateLastStatusMessage( results.ToString() );
        }

        private void UpdateDataViews()
        {
            RockContext rockContext = new RockContext();
            DataViewService dataViewService = new DataViewService( rockContext );
            int dataViewId;
            while ( dataViews.TryTake( out dataViewId ) )
            {
                try
                {
                    var dataView = dataViewService.Get( dataViewId );
                    dataView.PersistResult( sqlCommandTimeout );
                    dataView.PersistedLastRefreshDateTime = RockDateTime.Now;
                    rockContext.SaveChanges();
                    updatedDataViewCount++;
                }
                catch ( Exception e )
                {
                    ExceptionLogService.LogException( new Exception( "Error while persiting DataView. See inner excpeption for details.", e ) );
                }
            }
        }
    }
}
