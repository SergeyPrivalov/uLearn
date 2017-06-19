﻿using System;
using System.Linq;
using Graphite;
using log4net;
using System.Configuration;

namespace Metrics
{
	public class GraphiteMetricSender
	{
		private readonly ILog log = LogManager.GetLogger(typeof(GraphiteMetricSender));

		private readonly string service;
		private static string MachineName => Environment.MachineName.Replace(".", "_").ToLower();
		private static bool IsEnabled = !string.IsNullOrEmpty(ConfigurationManager.ConnectionStrings["statd"]?.ConnectionString);

		public GraphiteMetricSender(string service)
		{
			this.service = service;
		}

		/* Builds key "{service}.{machine_name}.{key}" */
		public static string BuildKey(string service, string key)
		{
			var parts = new[] { service, MachineName, key };
			parts = parts.Where(s => !string.IsNullOrEmpty(s)).ToArray();
			return string.Join(".", parts);
		}

		private string BuildKey(string key)
		{
			return BuildKey(service, key);
		}

		public void SendCount(string key, int value = 1, float sampling = 1)
		{
			if (!IsEnabled)
				return;

			var builtKey = BuildKey(key);
			log.Info($"Send count metric {builtKey}, value {value}");
			try
			{
				MetricsPipe.Current.Count(builtKey, value, sampling);
			}
			catch (Exception e)
			{
				log.Warn($"Can't send count metric {builtKey}, value {value}", e);
			}
		}

		public void SendTiming(string key, int value)
		{
			if (!IsEnabled)
				return;

			var builtKey = BuildKey(key);
			log.Info($"Send timing metric {builtKey}, value {value}");
			try
			{ 
				MetricsPipe.Current.Timing(builtKey, value);
			}
			catch (Exception e)
			{
				log.Warn($"Can't send timing metric {builtKey}, value {value}", e);
			}
		}

		public void SendGauge(string key, int value)
		{
			if (!IsEnabled)
				return;

			var builtKey = BuildKey(key);
			log.Info($"Send gauge metric {builtKey}, value {value}");
			try
			{ 
				MetricsPipe.Current.Gauge(builtKey, value);
			}
			catch (Exception e)
			{
				log.Warn($"Can't send gauge metric {builtKey}, value {value}", e);
			}
		}

		public void SendRaw(string key, int value)
		{
			if (!IsEnabled)
				return;

			var builtKey = BuildKey(key);
			log.Info($"Send raw metric {builtKey}, value {value}");
			try
			{ 
				MetricsPipe.Current.Raw(builtKey, value);
			}
			catch (Exception e)
			{
				log.Warn($"Can't send raw metric {builtKey}, value {value}", e);
			}
		}
	}
}