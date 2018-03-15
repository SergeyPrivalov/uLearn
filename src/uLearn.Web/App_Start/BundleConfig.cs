using System.Collections.Generic;
using System.Linq;
using System.Web.Optimization;
using AspNetBundling;

namespace uLearn.Web
{
	public class BundleConfig
	{
		// For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
		public static void RegisterBundles(BundleCollection bundles)
		{
			bundles.Add(JsBundle());

			// Use the development version of Modernizr to develop with and learn from. Then, when you're
			// ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
			bundles.Add(new ScriptBundle("~/modernizr.bundle.js").Include("~/Scripts/modernizr-*"));

			bundles.Add(CssBundle());
		}

		private static Bundle JsBundle()
		{
			var allScripts = GetLibrariesScripts().Concat(GetUlearnScripts()).ToArray();
			return new ScriptWithSourceMapBundle("~/scripts.bundle.js").Include(allScripts);
		}

		private static IEnumerable<string> GetLibrariesScripts()
		{
			return new[]
			{
				"~/Scripts/jquery-{version}.js",
				"~/Scripts/jquery-ui.min.js",
				"~/Scripts/jquery.unobtrusive-ajax*",
				"~/Scripts/notify-custom.min.js",
				"~/Scripts/jquery.query-object.js",
				"~/Scripts/jquery.color-2.1.0.min.js",
				"~/Scripts/jquery.validate.js",
				"~/Scripts/jquery.validate-vsdoc.js",
				"~/Scripts/jquery.validate.unobtrusive.js",
				"~/Scripts/jquery.validate.date.js",
				"~/Scripts/jquery.datetimepicker.full.min.js",
				"~/Scripts/jquery.datetimepicker.unobtrusive.js",
				"~/Scripts/jquery.event.move.js",
				"~/Scripts/jquery.smooth-scrolling.js",
				"~/Scripts/js.cookie.js",
				"~/tablesorter-master/js/jquery.tablesorter.js",
				"~/tablesorter-master/js/jquery.tablesorter.widgets.js",
				"~/tablesorter-master/js/widgets/widget-grouping.js",
				"~/Scripts/table-configurator.js",
				"~/flexslider/jquery.flexslider.js",
				"~/katex/katex.js",
				"~/codemirror/lib/codemirror.js",
				"~/codemirror/mode/clike/clike.js",
				"~/codemirror/mode/python/python.js",
				"~/codemirror/addon/hint/show-hint.js",
				"~/codemirror/addon/hint/cscompleter.js",
				"~/codemirror/addon/hint/csharp-hint.js",
				"~/codemirror/addon/edit/closebrackets.js",
				"~/codemirror/addon/edit/matchbrackets.js",
				"~/codemirror/addon/selection/active-line.js",
				"~/codemirror/addon/selection/mark-selection.js",
				"~/codemirror/addon/fold/foldcode.js",
				"~/Scripts/bootstrap.js",
				"~/Scripts/bootstrap.file-input.js",
				"~/Scripts/bootstrap-select.min.js",
				"~/Scripts/bootstrap-select.additional.js",
			};
		}

		private static IEnumerable<string> GetUlearnScripts()
		{
			return new[]
			{
				"~/Scripts/tooltips.js",
				"~/Scripts/clipboard.min.js",
				"~/Scripts/activate-clipboard.js",
				"~/Scripts/buttons.js",
				"~/Scripts/respond.js",
				"~/Scripts/slide-*",
				"~/Scripts/users-list.js",
				"~/Scripts/groups-editor.js",
				"~/Scripts/certificates-editor.js",
				"~/Scripts/analytics-highcharts.js",
				"~/Scripts/likely.js",
				"~/Scripts/analytics.js",
				"~/Scripts/course-statistics.js",
				"~/Scripts/ulearn-updates-invitation.js",
				"~/Scripts/suggest-mail-notifications.js",
				"~/Scripts/diagnostics.js",
				"~/Scripts/forms.replace-action.js",
				"~/Scripts/grader.js",
				"~/Scripts/notifications-settings.js",
				"~/Scripts/profile.js",
				"~/Scripts/email-is-not-confirmed.js",
				"~/Scripts/notifications.js",
				"~/Scripts/stepik.js",
				"~/Scripts/modals.js",
				"~/Scripts/connect-checkboxes.js",
				"~/Scripts/antiplagiarism.js"
			};
		}

		private static Bundle CssBundle()
		{
			return new StyleBundle("~/ulearn.bundle.css").Include(
				"~/Content/bootstrap.css",
				"~/Content/font-awesome.css",
				"~/Content/awesome-bootstrap-checkbox.css",
				"~/Content/bootstrap-select.min.css",
				"~/Content/bootstrap.refresh-animate.css",
				"~/Content/jquery.datetimepicker.min.css",
				"~/codemirror/lib/codemirror.css",
				"~/codemirror/theme/cobalt.css",
				"~/codemirror/addon/hint/show-hint.css",
				"~/Content/tablesorter.css",
				"~/tablesorter-master/css/widget.grouping.css",
				"~/Content/ulearn.css",
				"~/Content/bounce.css",
				"~/flexslider/flexslider.css",
				"~/Content/jquery-ui.min.css",
				"~/Content/site.css",
				"~/Content/buttons.css",
				"~/Content/likely.css",
				"~/Content/notifications.css",
				"~/Content/stepik.css",
				"~/Content/modals.css",
				"~/Content/antiplagiarism.css"
			);
		}
	}
}