using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI;
using Countersoft.Foundation.Commons.Extensions;
using Countersoft.Gemini.Commons;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Commons.Permissions;
using Countersoft.Gemini.Extensibility.Apps;
using Countersoft.Gemini.Infrastructure;
using Countersoft.Gemini.Infrastructure.Apps;
using Countersoft.Gemini.Models;
using System.Linq;
using System.Text;
using Countersoft.Gemini.Infrastructure.Helpers;
using Countersoft.Foundation.Commons.Enums;
using Countersoft.Gemini.Commons.Entity.Security;
using Countersoft.Gemini.Commons.Meta;
using Countersoft.Gemini;
using Countersoft.Gemini.Infrastructure.Managers;

namespace QuickEntry
{
    [AppType(AppTypeEnum.FullPage),
    AppGuid("341532F1-55C4-4399-9458-9DD37B8D4237"),
    AppControlGuid("5A3B3CF0-8A43-4B0C-B94B-74D447520A44"),
    AppAuthor("Countersoft"), AppKey("QuickEntry"),
    AppName("Quick Entry"),
    AppDescription("QuickEntry"),
    AppControlUrl("view"), AppRequiresCreatePermission(true)]
    public class QuickEntryController : BaseAppController
    {
        public override WidgetResult Caption(IssueDto issue = null)
        {
            return new WidgetResult() { Success = true, Markup = new WidgetMarkup(AppName) };
        }

        public override WidgetResult Show(IssueDto issue = null)
        {
            QuickEntryModel model = new QuickEntryModel();

            HttpSessionManager HttpSessionManager = new HttpSessionManager();

            IssuesGridFilter tmp = new IssuesGridFilter();
            var selectedProjects = new List<int>();

            try
            {
                if (CurrentCard.IsNew)
                {
                    tmp = new IssuesGridFilter(HttpSessionManager.GetFilter(CurrentCard.Id, CurrentCard.Filter));

                    if (tmp == null)
                    {
                        tmp = CurrentCard.Options[AppGuid].FromJson<IssuesGridFilter>();
                    }

                    selectedProjects = tmp.GetProjects(); 
                }
                else
                {
                    var cardOptions = CurrentCard.Options[AppGuid].FromJson<QuickEntryWorkspaceModel>();

                    selectedProjects.Add(cardOptions.projectId);
                }
            }
            catch (Exception ex)
            {
                tmp = new IssuesGridFilter(HttpSessionManager.GetFilter(CurrentCard.Id, IssuesFilter.CreateProjectFilter(UserContext.User.Entity.Id, UserContext.Project.Entity.Id)));

                selectedProjects = tmp.GetProjects();
            }
  
            int currentProjectId = selectedProjects.Count > 0 ? selectedProjects.First() : 0;

            var projects = ProjectManager.GetAppCreateableProjects(this);

            if (!projects.Any(s => s.Entity.Id == currentProjectId))
            {
                currentProjectId = projects.Count > 0 ? projects.First().Entity.Id : 0;
            }

            model.ProjectList = new SelectList(projects, "Entity.Id", "Entity.Name", currentProjectId);

            return new WidgetResult() { Success = true, Markup = new WidgetMarkup("Views/QuickEntry.cshtml", model) };
        }

        [AppUrl("createissues")]
        public ActionResult CreateIssues(string items, int projectId)
        {
            var projects = ProjectManager.GetAppCreateableProjects(this);

            if (!projects.Any(p => p.Entity.Id == projectId)) return JsonError("Project not found");

            var issueItems = items.FromJson<List<Item>>();

            if (issueItems == null || issueItems.Count == 0) return JsonError("No Items");

            UserContext.Project = ProjectManager.Get(projectId);

            if (UserContext.Project == null) return JsonError("Project not found");

            foreach (var issueItem in issueItems)
            {
                CreateIssue(issueItem, null);
            }

            return JsonSuccess();
        }

        private void CreateIssue(Item item, int? parentId)
        {
            var issue = new Issue { Title = item.text, ParentIssueId = parentId, ProjectId = CurrentProject.Entity.Id };

            if (string.IsNullOrWhiteSpace(issue.Title))
            {
                if (item.children.Any())
                    issue.Title = GetResource(ResourceKeys.Untitled);
                else
                {
                    return;
                }
            }

            var newIssue = IssueManager.Create(issue);

            UserManager.AddIssueAction(newIssue);

            foreach (var child in item.children)
            {
                CreateIssue(child, newIssue.Entity.Id);
            }
        }

    }

}
