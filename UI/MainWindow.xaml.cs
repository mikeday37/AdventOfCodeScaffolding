﻿using AdventOfCodeScaffolding;
using AdventOfCodeScaffolding.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using LX = System.Linq.Expressions;

namespace AdventOfCodeScaffolding.UI
{
    public partial class MainWindow : Window
    {
        public ChallengeInfo SelectedChallenge
        {
            get => (ChallengeInfo)GetValue(selectedChallengeDp);
            set
            {
                SetValue(selectedChallengeDp, value);
                UpdateTestResults(value);
            }
        }

        public IReadOnlyList<ChallengeInfo> Challenges { get; }

        public string Input { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Challenges = Assembly.GetEntryAssembly().GetTypes()
                .Where(x => typeof(ChallengeBase).IsAssignableFrom(x) && !x.IsAbstract)
                .Where(x => x.IsDefined(typeof(ChallengeAttribute), false))
                .Select(ChallengeInfo.FromType)
                .OrderBy(x => x.Day)
                .ToList();

            SelectedChallenge = Challenges.LastOrDefault();
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedChallenge == null)
                return;

            var instance = SelectedChallenge.Create();
            var pt1Result = Measure(() => instance.Part1(Input), out var pt1Metrics);
            var pt2Result = Measure(() => instance.Part2(Input), out var pt2Metrics);

            Part1.Update(pt1Result, pt1Metrics);
            Part2.Update(pt2Result, pt2Metrics);
        }

        private object Measure(Func<object> action, out Metrics metrics)
        {
            try
            {
                object result = null;
                metrics = Metrics.Measure(
                    maxMilliseconds: 150, 
                    minReps: 10, 
                    runOnceOnly: !EnableBenchmarking,
                    action: () =>
                {
                    var newResult = action();
                    if (result != null && !result.Equals(newResult))
                        throw new Exception($"Result differed between test runs! Old: {result}, New: {newResult}");

                    result = newResult;
                });

                return result;
            }
            catch (NotImplementedException)
            {
                metrics = Metrics.Empty;
                return "Not yet implemented";
            }
            catch { throw; }
        }

        private static TestResults GetTestResult(Action runTest)
        {
            try
            {
                runTest();
                return TestResults.Passed;
            }
            catch (NotImplementedException)
            {
                return TestResults.NotImplemented;
            }
            catch
            {
                return TestResults.Failed;
            }
        }

        public class ChallengeInfo
        {
            public int Day { get; }

            public string Name { get; }

            public Factory Create { get; }

            public static ChallengeInfo FromType(Type type)
            {
                var attr = type.GetCustomAttribute<ChallengeAttribute>(false);
                var name = $"Day {attr.DayNum} - {attr.Title}";
                var factory = CreateFactory(type);
                return new ChallengeInfo(attr.DayNum, name, factory);
            }

            private ChallengeInfo(int day, string name, Factory func)
            {
                Day = day;
                Name = name;
                Create = func;
            }

            private static Factory CreateFactory(Type challengeType)
            {
                var ctor = challengeType.GetConstructor(Type.EmptyTypes);
                var fac = LX.Expression.New(ctor);
                return LX.Expression.Lambda<Factory>(fac).Compile();
            }

            public delegate ChallengeBase Factory();
        }

        private void UpdateTestResults(ChallengeInfo value)
        {
            if (value == null)
                return;

            var instance = value.Create();

            Part1.TestResults = GetTestResult(() => instance.Part1Test());
            Part2.TestResults = GetTestResult(() => instance.Part2Test());
        }

        private static readonly DependencyProperty selectedChallengeDp =
            DependencyProperty.Register("SelectedChallenge",
            typeof(ChallengeInfo), typeof(MainWindow),
            new PropertyMetadata(null, (s, e) => ((MainWindow)s).UpdateTestResults((ChallengeInfo) e.NewValue)));

		public bool EnableBenchmarking
		{
			get { return (bool)GetValue(EnableBenchmarkingProperty); }
			set { SetValue(EnableBenchmarkingProperty, value); }
		}

		public static readonly DependencyProperty EnableBenchmarkingProperty =
			DependencyProperty.Register("EnableBenchmarking", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(true));
	}
}