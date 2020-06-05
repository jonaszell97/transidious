using System.Collections.Generic;
using UnityEngine;

namespace Transidious
{
    public class UndoStack
    {
        /// Represents a single action that can be un/redone.
        public class Action
        {
            /// Delegate for an action function.
            public delegate void ActionFunc();

            /// Perform the action.
            public ActionFunc Execute;

            /// Revert the action.
            public ActionFunc Revert;

            /// C'tor.
            public Action(ActionFunc execute, ActionFunc revert)
            {
                Execute = execute;
                Revert = revert;
            }
        }

        /// The undo stack.
        private readonly Stack<Action> _undoStack;

        /// The redo stack.
        private readonly Stack<Action> _redoStack;

        /// Whether or not the undo stack is consistent, i.e. an operation can be undone even
        /// after other operations have been performed.
        private readonly bool _consistent;

        /// C'tor.
        public UndoStack(bool consistent)
        {
            _undoStack = new Stack<Action>();
            _redoStack = new Stack<Action>();
            _consistent = consistent;
        }

        /// Perform an action and add it to the undo stack.
        public void PushAndExecute(Action.ActionFunc execute, Action.ActionFunc revert)
        {
            var action = new Action(execute, revert);
            action.Execute();

            _undoStack.Push(action);

            if (!_consistent)
            {
                _redoStack.Clear();
            }
        }

        /// Add an action to the undo stack without executing it.
        public void PushNoExecute(Action.ActionFunc execute, Action.ActionFunc revert)
        {
            var action = new Action(execute, revert);
            _undoStack.Push(action);
        }

        /// Whether or not we can undo an action right now.
        public bool CanUndo => _undoStack.Count > 0;
        
        /// Whether or not we can redo an action right now.
        public bool CanRedo => _redoStack.Count > 0;

        /// Undo an action.
        public void Undo()
        {
            Debug.Assert(CanUndo, "no action to undo!");

            var action = _undoStack.Pop();
            action.Revert();

            _redoStack.Push(action);
        }

        /// Redo an action.
        public void Redo()
        {
            Debug.Assert(CanRedo, "no action to redo!");

            var action = _redoStack.Pop();
            action.Execute();

            _undoStack.Push(action);
        }
    }
}