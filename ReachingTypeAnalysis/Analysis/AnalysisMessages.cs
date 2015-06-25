// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using ReachingTypeAnalysis.Communication;
using System;

namespace ReachingTypeAnalysis.Analysis
{
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
            return string.Format("CallMessage {0}", this.CallMessageInfo);
        }
    }

    [Serializable]
	internal class ReturnMessage : Message
	{
		public ReturnMessageInfo ReturnMessageInfo { get; private set; }
		internal ReturnMessage(IEntityDescriptor source, ReturnMessageInfo messageInfo)
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
			return string.Format("ReturnMessage {0}", this.ReturnMessageInfo);
		}
	}
}
