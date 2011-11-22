﻿using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Zyan.Communication;
using Zyan.Communication.Delegates;
using Zyan.Communication.Protocols.Ipc;

namespace Zyan.Tests
{
	#region Unit testing platform abstraction layer
#if NUNIT
	using NUnit.Framework;
	using TestClass = NUnit.Framework.TestFixtureAttribute;
	using TestMethod = NUnit.Framework.TestAttribute;
	using ClassInitializeNonStatic = NUnit.Framework.TestFixtureSetUpAttribute;
	using ClassInitialize = DummyAttribute;
	using ClassCleanupNonStatic = NUnit.Framework.TestFixtureTearDownAttribute;
	using ClassCleanup = DummyAttribute;
	using TestContext = System.Object;
	using AssertFailedException = NUnit.Framework.AssertionException;
#else
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ClassInitializeNonStatic = DummyAttribute;
	using ClassCleanupNonStatic = DummyAttribute;
#endif
	#endregion

	/// <summary>
	/// Test class for event filters.
	///</summary>
	[TestClass]
	public class EventFilterTests
	{
		#region Interfaces and components

		public delegate void CustomEventType(int firstArgument, string secondArgument);

		/// <summary>
		/// Sample server interface
		/// </summary>
		public interface ISampleServer
		{
			event EventHandler TestEvent;

			void RaiseTestEvent(EventArgs args = null);

			event EventHandler<SessionEventArgs> SessionBoundEvent;

			void RaiseSessionBoundEvent();

			event EventHandler<CustomEventArgs> CustomSessionBoundEvent;

			void RaiseCustomSessionBoundEvent(int value);

			event EventHandler<SampleEventArgs> SampleEvent;

			void RaiseSampleEvent(int value);

			event CustomEventType CustomEvent;

			void RaiseCustomEvent(int first = default(int), string second = default(string));
		}

		/// <summary>
		/// Sample server implementation
		/// </summary>
		public class SampleServer : ISampleServer
		{
			public event EventHandler TestEvent;

			public void RaiseTestEvent(EventArgs args)
			{
				if (TestEvent != null)
				{
					TestEvent(null, args);
				}
			}

			public event EventHandler<SessionEventArgs> SessionBoundEvent;

			public void RaiseSessionBoundEvent()
			{
				if (SessionBoundEvent != null)
				{
					SessionBoundEvent(null, new SessionEventArgs());
				}
			}

			public event EventHandler<CustomEventArgs> CustomSessionBoundEvent;

			public void RaiseCustomSessionBoundEvent(int value)
			{
				if (CustomSessionBoundEvent != null)
				{
					CustomSessionBoundEvent(null, new CustomEventArgs { Value = value });
				}
			}

			public event EventHandler<SampleEventArgs> SampleEvent;

			public void RaiseSampleEvent(int value)
			{
				if (SampleEvent != null)
				{
					SampleEvent(null, new SampleEventArgs { Value = value });
				}
			}

			public event CustomEventType CustomEvent;

			public void RaiseCustomEvent(int first = default(int), string second = default(string))
			{
				if (CustomEvent != null)
				{
					CustomEvent(first, second);
				}
			}
		}

		/// <summary>
		/// Custom session-bound event arguments.
		/// </summary>
		[Serializable]
		public class CustomEventArgs : SessionEventArgs
		{
			public int Value { get; set; }
		}

		/// <summary>
		/// Custom event argument class for event filtering.
		/// </summary>
		[Serializable]
		public class SampleEventArgs : EventArgs
		{
			public int Value { get; set; }
		}

		/// <summary>
		/// Event filter for the SampleEventArgs class.
		/// </summary>
		[Serializable]
		public class SampleEventFilter : EventFilterBase<SampleEventArgs>
		{
			public SampleEventFilter(params int[] values)
			{
				Values = new HashSet<int>(values);
			}

			public HashSet<int> Values { get; private set; }

			protected override bool AllowInvocation(object sender, SampleEventArgs args)
			{
				return Values.Contains(args.Value);
			}
		}

		/// <summary>
		/// Event filter for EventArgs class.
		/// </summary>
		[Serializable]
		public class TestEventFilter : EventFilterBase<EventArgs>
		{
			protected override bool AllowInvocation(object sender, EventArgs args)
			{
				return args == null;
			}
		}

		/// <summary>
		/// Event filter for CustomEventType.
		/// </summary>
		[Serializable]
		public class CustomEventFilter : IEventFilter
		{
			public CustomEventFilter(params string[] templates)
			{
				Templates = new HashSet<string>(templates);
			}

			private ISet<string> Templates { get; set; }

			public bool AllowInvocation(params object[] parameters)
			{
				var text = string.Format("{0}{1}", parameters);
				return Templates.Contains(text);
			}
		}

		#endregion

		public TestContext TestContext { get; set; }

		static ZyanComponentHost ZyanHost { get; set; }

		static ZyanConnection ZyanConnection { get; set; }

		[ClassInitializeNonStatic]
		public void Initialize()
		{
			StartServer(null);
		}

		[ClassCleanupNonStatic]
		public void Cleanup()
		{
		}

		[ClassInitialize]
		public static void StartServer(TestContext ctx)
		{
			var serverSetup = new IpcBinaryServerProtocolSetup("EventFilterTest");
			ZyanHost = new ZyanComponentHost("EventFilterServer", serverSetup);
			ZyanHost.RegisterComponent<ISampleServer, SampleServer>(ActivationType.Singleton);

			var clientSetup = new IpcBinaryClientProtocolSetup();
			ZyanConnection = new ZyanConnection("ipc://EventFilterTest/EventFilterServer", clientSetup);
		}

		[ClassCleanup]
		public static void StopServer()
		{
			ZyanConnection.Dispose();
			ZyanHost.Dispose();
		}

		[TestMethod]
		public void SubscriptionUnsubscription_RegressionTest()
		{
			var handled = false;
			var eventHandler = new EventHandler((s, e) => handled = true);

			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.TestEvent += eventHandler;
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			proxy.TestEvent -= eventHandler; // unsubscription #1
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);

			proxy.TestEvent += eventHandler;
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsTrue(handled);

			proxy.TestEvent -= eventHandler; // unsubscription #2
			handled = false;

			proxy.RaiseTestEvent();
			Assert.IsFalse(handled);
		}

		/* Session-bound events */

		[TestMethod]
		public void SessionBoundEvents_AreBoundToSessions()
		{
			// start a new session
			using (var conn = new ZyanConnection(ZyanConnection.ServerUrl, new IpcBinaryClientProtocolSetup()))
			{
				var proxy1 = ZyanConnection.CreateProxy<ISampleServer>();
				var proxy2 = conn.CreateProxy<ISampleServer>();

				var handled1 = false;
				var handled2 = false;

				proxy1.SessionBoundEvent += (s, args) => handled1 = true;
				proxy2.SessionBoundEvent += (s, args) => handled2 = true;

				proxy1.RaiseSessionBoundEvent();
				Assert.IsTrue(handled1);
				Assert.IsFalse(handled2);

				handled1 = false;

				proxy2.RaiseSessionBoundEvent();
				Assert.IsFalse(handled1);
				Assert.IsTrue(handled2);
			}
		}

		[TestMethod]
		public void EventsWithArgumentsDerivedFromSessionBoundEvents_AreBoundToSessions()
		{
			// start a new session
			using (var conn = new ZyanConnection(ZyanConnection.ServerUrl, new IpcBinaryClientProtocolSetup()))
			{
				var proxy1 = ZyanConnection.CreateProxy<ISampleServer>();
				var proxy2 = conn.CreateProxy<ISampleServer>();

				var handled1 = 0;
				var handled2 = 0;

				proxy1.CustomSessionBoundEvent += (s, args) => handled1 = args.Value;
				proxy2.CustomSessionBoundEvent += (s, args) => handled2 = args.Value;

				proxy1.RaiseCustomSessionBoundEvent(123);
				Assert.AreEqual(123, handled1);
				Assert.AreEqual(0, handled2);

				proxy2.RaiseCustomSessionBoundEvent(321);
				Assert.AreEqual(123, handled1);
				Assert.AreEqual(321, handled2);
			}
		}

		/* Client-side (local) event filters */

		[TestMethod]
		public void FilteredEventHandlerUsingFactorySyntax_FiltersEventsLocally()
		{
			// prepare event handler, attach event filter
			var handledValue = 0;
			var sample = new SampleServer();
			sample.SampleEvent += FilteredEventHandler.Create((object sender, SampleEventArgs args) => handledValue = args.Value, new SampleEventFilter(321));

			// raise events, check results
			sample.RaiseSampleEvent(123); // filtered out
			Assert.AreEqual(0, handledValue);

			sample.RaiseSampleEvent(321);
			Assert.AreEqual(321, handledValue);

			handledValue = 111;
			sample.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(111, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerUsingFluentSyntax_FiltersEventsLocally()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach client-side event filter
			var sample = new SampleServer();
			sample.SampleEvent += handler.AddFilter(new SampleEventFilter(321));

			// raise events, check results
			sample.RaiseSampleEvent(123); // filtered out
			Assert.AreEqual(0, handledValue);

			sample.RaiseSampleEvent(321);
			Assert.AreEqual(321, handledValue);

			handledValue = 111;
			sample.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(111, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerUsingCombinedFilter_FiltersEventsLocally()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach client-side event filter
			var sample = new SampleServer();
			sample.SampleEvent += handler
				.AddFilter(new SampleEventFilter(321, 123, 111))
				.AddFilter(new SampleEventFilter(333, 123, 222)); // 123 will pass both filters

			// raise events, check results
			sample.RaiseSampleEvent(321); // filtered out
			Assert.AreEqual(0, handledValue);

			sample.RaiseSampleEvent(123);
			Assert.AreEqual(123, handledValue);

			handledValue = 111;
			sample.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(111, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerOfTypeEventHandler_FiltersEventsLocally()
		{
			// prepare event handler
			var handled = false;
			var handler = new EventHandler((sender, args) => handled = true);

			// attach client-side event filter
			var sample = new SampleServer();
			sample.TestEvent += handler.AddFilter(new TestEventFilter());

			// raise events, check results
			sample.RaiseTestEvent(EventArgs.Empty); // filtered out
			Assert.IsFalse(handled);

			sample.RaiseTestEvent(null);
			Assert.IsTrue(handled);

			handled = false;
			sample.RaiseTestEvent(new EventArgs()); // filtered out
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void FilteredEventHandlerOfCustomType_FiltersEventsLocally()
		{
			// prepare event handler
			var firstArgument = 0;
			var secondArgument = string.Empty;
			var handled = false;
			var handler = new CustomEventType((first, second) =>
			{
				firstArgument = first;
				secondArgument = second;
				handled = true;
			});

			// attach client-side event filter
			var sample = new SampleServer();
			sample.CustomEvent += FilteredEventHandler.Create(handler, new CustomEventFilter("3.14"));

			// raise events, check results
			sample.RaiseCustomEvent(1, string.Empty); // filtered out
			Assert.IsFalse(handled);

			sample.RaiseCustomEvent(3, ".14");
			Assert.IsTrue(handled);
			Assert.AreEqual(3, firstArgument);
			Assert.AreEqual(".14", secondArgument);

			handled = false;
			sample.RaiseCustomEvent(); // filtered out
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void FilteredCustomHandlerUsingCombinedFilter_FiltersEventsLocally()
		{
			// prepare event handler
			var firstArgument = 0;
			var secondArgument = string.Empty;
			var handled = false;
			var handler = new CustomEventType((first, second) =>
			{
				firstArgument = first;
				secondArgument = second;
				handled = true;
			});

			// initialize event filter
			handler = FilteredEventHandler.Create(handler, new CustomEventFilter("2.718", "1.618"));
			handler = FilteredEventHandler.Create(handler, new CustomEventFilter("3.14", "2.718"));

			// attach client-side event filter
			var sample = new SampleServer();
			sample.CustomEvent += handler;

			// raise events, check results
			sample.RaiseCustomEvent(3, ".14"); // filtered out
			Assert.IsFalse(handled);

			sample.RaiseCustomEvent(2, ".718");
			Assert.IsTrue(handled);
			Assert.AreEqual(2, firstArgument);
			Assert.AreEqual(".718", secondArgument);

			handled = false;
			sample.RaiseCustomEvent(1, ".618"); // filtered out
			Assert.IsFalse(handled);
		}

		/* Server-side (remote) event filters */

		[TestMethod]
		public void FilteredEventHandlerUsingFactorySyntax_FiltersEventsRemotely()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach server-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.SampleEvent += FilteredEventHandler.Create(handler, new SampleEventFilter(123));

			// raise events
			proxy.RaiseSampleEvent(111); // filtered out
			Assert.AreEqual(0, handledValue);

			proxy.RaiseSampleEvent(123);
			Assert.AreEqual(123, handledValue);

			handledValue = 222;
			proxy.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(222, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerUsingFluentSyntax_FiltersEventsRemotely()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach server-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.SampleEvent += handler.AddFilter(new SampleEventFilter(123));

			// raise events
			proxy.RaiseSampleEvent(111); // filtered out
			Assert.AreEqual(0, handledValue);

			proxy.RaiseSampleEvent(123);
			Assert.AreEqual(123, handledValue);

			handledValue = 222;
			proxy.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(222, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerUsingCombinedFilter_FiltersEventsRemotely()
		{
			// prepare event handler
			var handledValue = 0;
			var handler = new EventHandler<SampleEventArgs>((sender, args) => handledValue = args.Value);

			// attach server-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.SampleEvent += handler
				.AddFilter(new SampleEventFilter(3, 5, 7))
				.AddFilter(new SampleEventFilter(9, 6, 3))
				.AddFilter(new SampleEventFilter(1, 2, 3))
				.AddFilter(new SampleEventFilter(5, 4, 3)); // 3 will pass all filters

			// raise events, check results
			proxy.RaiseSampleEvent(321); // filtered out
			Assert.AreEqual(0, handledValue);

			proxy.RaiseSampleEvent(3);
			Assert.AreEqual(3, handledValue);

			handledValue = 111;
			proxy.RaiseSampleEvent(456); // filtered out
			Assert.AreEqual(111, handledValue);
		}

		[TestMethod]
		public void FilteredEventHandlerOfTypeEventHandler_FiltersEventsRemotely()
		{
			// prepare event handler
			var handled = false;
			var handler = new EventHandler((sender, args) => handled = true);

			// attach server-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.TestEvent += handler.AddFilter(new TestEventFilter());

			// raise events, check results
			proxy.RaiseTestEvent(EventArgs.Empty); // filtered out
			Assert.IsFalse(handled);

			proxy.RaiseTestEvent(null);
			Assert.IsTrue(handled);

			handled = false;
			proxy.RaiseTestEvent(new EventArgs()); // filtered out
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void FilteredEventHandlerOfCustomType_FiltersEventsRemotely()
		{
			// prepare event handler
			var firstArgument = 0;
			var secondArgument = string.Empty;
			var handled = false;
			var handler = new CustomEventType((first, second) =>
			{
				firstArgument = first;
				secondArgument = second;
				handled = true;
			});

			// attach server-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.CustomEvent += FilteredEventHandler.Create(handler, new CustomEventFilter("2.71828"));

			// raise events, check results
			proxy.RaiseCustomEvent(1, string.Empty); // filtered out
			Assert.IsFalse(handled);

			proxy.RaiseCustomEvent(2, ".71828");
			Assert.IsTrue(handled);
			Assert.AreEqual(2, firstArgument);
			Assert.AreEqual(".71828", secondArgument);

			handled = false;
			proxy.RaiseCustomEvent(); // filtered out
			Assert.IsFalse(handled);
		}

		[TestMethod]
		public void FilteredCustomHandlerUsingCombinedFilter_FiltersEventsRemotely()
		{
			// prepare event handler
			var firstArgument = 0;
			var secondArgument = string.Empty;
			var handled = false;
			var handler = new CustomEventType((first, second) =>
			{
				firstArgument = first;
				secondArgument = second;
				handled = true;
			});

			// initialize event filter
			handler = FilteredEventHandler.Create(handler, new CustomEventFilter("2.718", "1.618"));
			handler = FilteredEventHandler.Create(handler, new CustomEventFilter("3.14", "2.718"));

			// attach client-side event filter
			var proxy = ZyanConnection.CreateProxy<ISampleServer>();
			proxy.CustomEvent += handler;

			// raise events, check results
			proxy.RaiseCustomEvent(3, ".14"); // filtered out
			Assert.IsFalse(handled);

			proxy.RaiseCustomEvent(2, ".718");
			Assert.IsTrue(handled);
			Assert.AreEqual(2, firstArgument);
			Assert.AreEqual(".718", secondArgument);

			handled = false;
			proxy.RaiseCustomEvent(1, ".618"); // filtered out
			Assert.IsFalse(handled);
		}
	}
}
