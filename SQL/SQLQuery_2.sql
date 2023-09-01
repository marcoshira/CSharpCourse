USE DotNetCourseDatabase
GO

ALTER PROCEDURE TutorialAppSchema.spUsers_Get
    @UserId INT = NULL
AS
BEGIN
    SELECT [Users].[UserId],
    [Users].[FirstName],
    [Users].[LastName],
    [Users].[Email],
    [Users].[Gender],
    [Users].[Active],
    [UserSalary].[Salary],
    [UserJobInfo].Department,
    UserJobInfo.JobTitle
    FROM TutorialAppSchema.Users AS Users
    LEFT JOIN TutorialAppSchema.UserSalary AS UserSalary ON UserSalary.UserId = Users.UserId
    LEFT JOIN TutorialAppSchema.UserJobInfo AS UserJobInfo ON UserJobInfo.UserId = Users.UserId
    WHERE Users.UserId = @UserID
END
GO

/*EXEC TutorialAppSchema.spUsers_Get @UserId=4*/

CREATE OR ALTER PROCEDURE TutorialAppSchema.spUsers_Upsert
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50),
    @Email NVARCHAR(50),
    @Gender NVARCHAR(50),
    @JobTitle NVARCHAR(50),
    @Department NVARCHAR(50),
    @Salary DECIMAL(18, 4),
    @Active BIT = 1,
    @UserID INT = NULL
AS
BEGIN
    IF NOT EXISTS (SELECT * FROM TutorialAppSchema.Users WHERE UserID = @UserID)
        BEGIN
            IF NOT EXISTS (SELECT * FROM TutorialAppSchema.Users WHERE Email = @Email)
                BEGIN
                    DECLARE @OutputUserId INT

                    INSERT INTO TutorialAppSchema.Users(
                        [FirstName],
                        [LastName],
                        [Email],
                        [Gender],
                        [Active]
                    ) VALUES (
                        @FirstName,
                        @LastName,
                        @Email,
                        @Gender,
                        @Active
                    )

                    SET @OutputUserId = @@IDENTITY

                    INSERT INTO TutorialAppSchema.UserSalary(
                        UserId, Salary
                    ) VALUES (
                        @OutputUserId, @Salary
                    )

                    INSERT INTO TutorialAppSchema.UserJobInfo(
                        UserId, Department, JobTitle
                    ) VALUES (
                        @OutputUserId, @Department, @JobTitle
                    )
                END
        END
    ELSE
        BEGIN
            UPDATE TutorialAppSchema.Users
                SET
                    FirstName = @FirstName,
                    LastName = @LastName,
                    Email = @Email,
                    Gender = @Gender,
                    Active = @Active
                WHERE UserID = @UserID
            UPDATE TutorialAppSchema.UserSalary
                SET
                    Salary = @Salary
                WHERE UserId = @UserID
            UPDATE TutorialAppSchema.UserJobInfo
                SET
                    Department = @Department,
                    JobTitle = @JobTitle
                WHERE UserId = @UserID
        END
END
GO

CREATE PROCEDURE TutorialAppSchema.spUser_Delete
    @UserId INT
AS
BEGIN
    DELETE FROM TutorialAppSchema.Users WHERE UserId = @UserId

    DELETE FROM TutorialAppSchema.UserSalary WHERE UserId = @UserId

    DELETE FROM TutorialAppSchema.UserJobInfo WHERE UserId = @UserId
END
GO

CREATE OR ALTER PROCEDURE TutorialAppSchema.spPosts_Get
    @UserId INT = NULL,
    @PostId INT = NULL,
    @SearchValue NVARCHAR(MAX) = NULL
AS
BEGIN
    SELECT [PostId],
    [UserId],
    [PostTitle],
    [PostContent],
    [PostCreated],
    [PostUpdated] FROM TutorialAppSchema.Posts AS Posts
    WHERE Posts.UserId = ISNULL(@UserID, Posts.UserID)
    AND Posts.PostId = ISNULL(@PostId, Posts.PostId)
    AND (@SearchValue IS NULL
        OR Posts.PostContent LIKE '%' + @SearchValue + '%'
        OR Posts.PostTitle LIKE '%' + @SearchValue + '%'
    )

END
GO

/* EXEC TutorialAppSchema.spPosts_Get @PostId = 4 */