﻿namespace WorkoutWotch.UnitTests.Models.Actions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Kent.Boogaart.PCLMock;
    using NUnit.Framework;
    using WorkoutWotch.Models;
    using WorkoutWotch.Models.Actions;
    using WorkoutWotch.UnitTests.Models.Mocks;
    using WorkoutWotch.UnitTests.Services.Logger.Mocks;

    [TestFixture]
    public class DoNotAwaitActionFixture
    {
        [Test]
        public void ctor_throws_if_logger_service_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new DoNotAwaitAction(null, new ActionMock()));
        }

        [Test]
        public void ctor_throws_if_inner_action_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new DoNotAwaitAction(new LoggerServiceMock(), null));
        }

        [Test]
        public void duration_always_returns_zero_regardless_of_inner_action_duration()
        {
            var action = new ActionMock();
            action.When(x => x.Duration).Return(TimeSpan.FromSeconds(3));

            var sut = new DoNotAwaitAction(new LoggerServiceMock(MockBehavior.Loose), action);
            Assert.AreEqual(TimeSpan.Zero, sut.Duration);
        }

        [Test]
        public void execute_async_throws_if_the_context_is_null()
        {
            var sut = new DoNotAwaitAction(new LoggerServiceMock(MockBehavior.Loose), new ActionMock(MockBehavior.Loose));
            Assert.Throws<ArgumentNullException>(async () => await sut.ExecuteAsync(null));
        }

        [Test]
        public void execute_async_does_not_wait_for_inner_actions_execution_to_complete_before_itself_completing()
        {
            var action = new ActionMock();
            var tcs = new TaskCompletionSource<bool>();
            action.When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>())).Return(tcs.Task);

            var sut = new DoNotAwaitAction(new LoggerServiceMock(MockBehavior.Loose), action);
            var task = sut.ExecuteAsync(new ExecutionContext());

            Assert.True(task.Wait(TimeSpan.FromSeconds(3)));
        }

        [Test]
        public async Task execute_async_logs_any_error_raised_by_the_inner_action()
        {
            var waitHandle = new ManualResetEventSlim();
            var logger = new LoggerMock(MockBehavior.Loose);
            logger.When(x => x.Error(It.IsAny<string>())).Do(waitHandle.Set);
            var loggerService = new LoggerServiceMock(MockBehavior.Loose);
            loggerService.When(x => x.GetLogger(It.IsAny<Type>())).Return(logger);
            var action = new ActionMock(MockBehavior.Loose);
            action
                .When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>()))
                .Return(Task.Run(() => { throw new InvalidOperationException("Something bad happened"); }));

            var sut = new DoNotAwaitAction(loggerService, action);
            await sut.ExecuteAsync(new ExecutionContext());

            Assert.True(waitHandle.Wait(TimeSpan.FromSeconds(3)));
        }
    }
}