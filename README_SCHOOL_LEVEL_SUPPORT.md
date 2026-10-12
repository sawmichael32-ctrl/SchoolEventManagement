# School Event Management System — JHS / SHS support

## What's changed
- Student registration now captures School Level (College, SHS, JHS), grade/year level, program/course/strand, and section.
- SHS supports Grade 11/12 and a strand/program such as ICT or STEM. JHS supports Grades 7–10. College supports year level and course/program.
- Admin event create/edit screens can target all school levels or a specific level, grade/year, program/strand/course, and section. Empty target filters mean all values within that school level.
- The server validates event eligibility; changing the form in the browser cannot bypass an event's target restrictions.
- Registration ticket and attendance report now show school level and grade/year details.
- Existing event rows migrate with target school level `All`, and existing registration rows migrate with school level `College` to preserve compatibility.

## Apply the database migration
1. Open `SchoolEventManagement.slnx` in Visual Studio.
2. Open **Tools → NuGet Package Manager → Package Manager Console**.
3. Ensure `SchoolEventManagement` is selected as the Default project.
4. Run:
   ```powershell
   Update-Database
   ```
5. Build the solution and test registration for a College, SHS, and JHS event.

If Visual Studio reports that the database is not reachable, check the `DefaultConnection` in `appsettings.json`.

## Notes
- The existing `Course` database field is retained for compatibility with teacher course reports. For SHS, it stores the strand/program (e.g. ICT or STEM); for College, it stores the course/program.
- A registration is stored per event, as in the original design, so students provide their school details when registering for each event.
- This package has not been compiled in this environment because the .NET SDK is not installed here. Build and test it in Visual Studio before using it with live data.
