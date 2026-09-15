/**
 * The .NET equivalent is appsettings.json — except Angular bakes this into the
 * bundle at BUILD time, while .NET reads its config at startup.
 */
export const environment = {
  production: false,
  // MUST match the API's http port in
  // backend/TaskManagement.Api/Properties/launchSettings.json and in that
  // project's appsettings.Development.json "Urls". See plan.md section 2.1.
  apiBaseUrl: 'http://localhost:5178',
};
