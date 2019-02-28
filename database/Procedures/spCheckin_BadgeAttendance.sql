/*
<doc>
	<summary>
 		This function returns the attendance data needed for the Attendance Badge. If no family role (adult/child)
		is given it is looked up.  If the individual is an adult it will return family attendance if it's a child
		it will return the individual's attendance. If a person is in two families once as a child once as an
		adult it will pick the first role it finds.
	</summary>
	<returns>
		* AttendanceCount
		* SundaysInMonth
		* Month
		* Year
	</returns>
	<param name="PersonId" datatype="int">Person the badge is for</param>
	<param name="Role Guid" datatype="uniqueidentifier">The role of the person in the family (optional)</param>
	<param name="Reference Date" datatype="datetime">A date in the last month for the badge (optional, default is today)</param>
	<param name="Number of Months" datatype="int">Number of months to display (optional, default is 24)</param>
	<remarks>	
		Uses the following constants:
			* Group Type - Family: 790E3215-3B10-442B-AF69-616C0DCB998E
			* Group Role - Adult: 2639F9A5-2AAE-4E48-A8C3-4FFE86681E42
			* Group Role - Child: C8B1814F-6AA7-4055-B2D7-48FE20429CB9
	</remarks>
	<code>
		EXEC [dbo].[spCheckin_BadgeAttendance] 2 -- Ted Decker (adult)
		EXEC [dbo].[spCheckin_BadgeAttendance] 4 -- Noah Decker (child)
	</code>
</doc>
*/

ALTER PROCEDURE [dbo].[spCheckin_BadgeAttendance]
	@PersonId int 
	, @RoleGuid uniqueidentifier = null
	, @ReferenceDate datetime = null
	, @MonthCount int = 24
AS
BEGIN

	DECLARE @cROLE_ADULT uniqueidentifier = '2639F9A5-2AAE-4E48-A8C3-4FFE86681E42'
	DECLARE @cROLE_CHILD uniqueidentifier = 'C8B1814F-6AA7-4055-B2D7-48FE20429CB9'
	DECLARE @cGROUP_TYPE_FAMILY uniqueidentifier = '790E3215-3B10-442B-AF69-616C0DCB998E'
    DECLARE @StartDay datetime
	DECLARE @LastDay datetime

	-- if role (adult/child) is unknown determine it
	IF (@RoleGuid IS NULL)
	BEGIN
		SELECT TOP 1 @RoleGuid = gtr.[Guid] 
			FROM [GroupTypeRole] gtr
				INNER JOIN [GroupMember] gm ON gm.[GroupRoleId] = gtr.[Id]
				INNER JOIN [Group] g ON g.[Id] = gm.[GroupId]
			WHERE gm.[PersonId] = @PersonId 
				AND g.[GroupTypeId] = (SELECT [ID] FROM [GroupType] WHERE [Guid] = @cGROUP_TYPE_FAMILY)
	END

	-- if start date null get today's date
	IF (@ReferenceDate IS NULL)
		:SET @ReferenceDate = getdate()
	END

	-- set data boundaries
	SET @LastDay = dbo.ufnUtility_GetLastDayOfMonth(@ReferenceDate) -- last day is most recent day

	-- make sure last day is not in future (in case there are errant checkin data)
	-- Moved above @startDay so that month calculation isn't effected by errant data.
	IF (@LastDay > getdate())
		SET @LastDay = getdate()

	SET @StartDay = DATEADD(M, DATEDIFF(M, 0, DATEADD(month, ((@MonthCount -1) * -1), @LastDay)), 0) -- start day is the 1st of the first full month of the oldest day

    DECLARE @familyMemberPersonIds table ([PersonId] int); 

	IF (@RoleGuid = @cROLE_ADULT)
		INSERT INTO @familyMemberPersonIds SELECT [Id] FROM [dbo].[ufnCrm_FamilyMembersOfPersonId](@PersonId)
	ELSE IF (@RoleGuid = @cROLE_CHILD)
		INSERT INTO @familyMemberPersonIds SELECT @PersonId

	-- query for attendance data
	SELECT 
		COUNT(b.[Attended]) AS [AttendanceCount]
		, (SELECT dbo.ufnUtility_GetNumberOfSundaysInMonth(DATEPART(year, b.[SundayDate]), DATEPART(month, b.[SundayDate]), 'True' )) AS [SundaysInMonth]
		, DATEPART(month, b.[SundayDate]) AS [Month]
		, DATEPART(year, b.[SundayDate]) AS [Year]
	FROM (
		SELECT
			s.[SundayDate], a.[Attended]
		FROM
			dbo.ufnUtility_GetSundaysBetweenDates(@StartDay, @LastDay) s
			LEFT OUTER JOIN (
				SELECT
					DISTINCT ao.[SundayDate], 1 as [Attended]
				FROM
					[AttendanceOccurrence] ao
					INNER JOIN (SELECT [Id] FROM [dbo].[ufnCheckin_WeeklyServiceGroups]()) wg ON ao.[GroupId] = wg.[Id]
					INNER JOIN [Attendance] a ON ao.[Id] = a.[OccurrenceId] AND a.[DidAttend] = 1
					INNER JOIN [PersonAlias] pa ON a.[PersonAliasId] = pa.[Id] AND pa.[PersonId] IN (SELECT [PersonId] FROM @familyMemberPersonIds)
				WHERE
					ao.[OccurrenceDate] BETWEEN @StartDay AND @LastDay
			) a ON a.[SundayDate] = s.[SundayDate]
	) b
	GROUP BY DATEPART(month, b.[SundayDate]), DATEPART(year, b.[SundayDate])
	OPTION (MAXRECURSION 1000)
END
