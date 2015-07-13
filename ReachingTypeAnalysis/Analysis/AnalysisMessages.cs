// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using ReachingTypeAnalysis.Communication;
using System;

namespace ReachingTypeAnalysis.Analysis
{
    [Serializable]
    public abstract class Message : IMessage
    {
        public IEntityDescriptor Source { get; private set; }

        internal Message(IEntityDescriptor source)
        {
            this.Source = source;
        }

        public abstract MessageHandler Handler();
    }

    [Serializable]
    internal class CallerMessage : Message
    {
        public CallMessageInfo CallMessageInfo { get; private set; }

        internal CallerMessage(IEntityDescriptor source, CallMessageInfo messageInfo)
            : base(source)
        {
            this.CallMessageInfo = messageInfo;
        }

        public override MessageHandler Handler()
        {
            return (MessageHandler)Delegate.CreateDelegate(typeof(Func<MethodEntity, IMessage>),
                             typeof(MethodEntity).GetMethod("ProcessMessage"));
        }

        public override string ToString()
        {
            return this.CallMessageInfo.ToString();
        }
    }

    [Serializable]
	internal class CalleeMessage : Message
	{
		public ReturnMessageInfo ReturnMessageInfo { get; private set; }
		internal CalleeMessage(IEntityDescriptor source, ReturnMessageInfo messageInfo)
			: base(source)
		{
			this.ReturnMessageInfo = messageInfo;
		}

		public override MessageHandler Handler()
		{
			throw new NotImplementedException();
		}

		public override string ToString()
		{
			return this.ReturnMessageInfo.ToString();
		}
	}
}
