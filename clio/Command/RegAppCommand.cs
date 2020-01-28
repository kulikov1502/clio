﻿using System;
using Creatio.Client;
using CommandLine;
using Clio.Common;
using Clio.UserEnvironment;

namespace Clio.Command
{
	[Verb("reg-web-app", Aliases = new string[] { "reg", "cfg" }, HelpText = "Configure a web application settings")]
	public class RegAppOptions : EnvironmentOptions
	{
		[Value(0, MetaName = "Name", Required = false, HelpText = "Name of configured application")]
		public string Name { get => Environment; set { Environment = value; } }

		[Option('a', "ActiveEnvironment", Required = false, HelpText = "Set a web application by default")]
		public string ActiveEnvironment { get; set; }

		[Option('s', "Safe", Required = false, HelpText = "Safe action in this enviroment")]
		public string Safe { get; set; }

		public bool? SafeValue
		{
			get
			{
				if (!string.IsNullOrEmpty(Safe)) {
					bool result;
					if (bool.TryParse(Safe, out result)) {
						return result;
					}
				}
				return null;
			}
		}

		[Option('c', "dev", Required = false, HelpText = "Developer mode state for environment")]
		public string DevMode { get; set; }

		public bool? IsDevMode
		{
			get
			{
				if (!string.IsNullOrEmpty(DevMode)) {
					bool result;
					if (bool.TryParse(DevMode, out result)) {
						return result;
					}
				}
				return null;
			}
		}

	}

	public class RegAppCommand: Command<RegAppOptions>
	{
		private readonly ISettingsRepository _settingsRepository;
		private readonly IApplicationClientFactory _applicationClientFactory;

		public RegAppCommand(ISettingsRepository settingsRepository, IApplicationClientFactory applicationClientFactory) {
			_settingsRepository = settingsRepository;
			_applicationClientFactory = applicationClientFactory;
		}

		public override int Execute(RegAppOptions options) {
			try {
				var environment = new EnvironmentSettings {
					Login = options.Login,
					Password = options.Password,
					Uri = options.Uri,
					Maintainer = options.Maintainer,
					Safe = options.SafeValue.HasValue ? options.SafeValue : false,
					IsNetCore = options.IsNetCore.HasValue ? options.IsNetCore.Value : false,
					DeveloperModeEnabled = options.IsDevMode
				};
				if (!string.IsNullOrWhiteSpace(options.ActiveEnvironment)) {
					if (_settingsRepository.IsEnvironmentExists(options.ActiveEnvironment)) {
						_settingsRepository.SetActiveEnvironment(options.ActiveEnvironment);
						Console.WriteLine($"Active environment set to {options.ActiveEnvironment}");
						return 0;
					} else {
						throw new Exception($"Not found environment {options.ActiveEnvironment} in settings");
					}
				}
				_settingsRepository.ConfigureEnvironment(options.Name, environment);
				environment = _settingsRepository.GetEnvironment(options.Name);
				_settingsRepository.ShowSettingsTo(Console.Out, options.Name);
				Console.WriteLine();
				Console.WriteLine($"Try login to {environment.Uri} with {environment.Login} credentials ...");
				var creatioClient = _applicationClientFactory.CreateClient(environment);
				creatioClient.Login();
				Console.WriteLine($"Login successfull");
				return 0;
			} catch (Exception e) {
				Console.WriteLine($"{e.Message}");
				return 1;
			}
		}
	}
}
