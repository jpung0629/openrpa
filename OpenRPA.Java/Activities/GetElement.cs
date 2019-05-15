﻿using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Java
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.getjavaelement.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    //[designer.ToolboxTooltip(Text = "Find an Windows UI element based on xpath selector")]
    public class GetElement : NativeActivity, System.Activities.Presentation.IActivityTemplateFactory
    {
        [System.ComponentModel.Browsable(false)]
        public ActivityAction<JavaElement> Body { get; set; }
        public InArgument<int> MaxResults { get; set; }
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }
        public InArgument<JavaElement> From { get; set; }
        public OutArgument<JavaElement[]> Elements { get; set; }
        private Variable<IEnumerator<JavaElement>> _elements = new Variable<IEnumerator<JavaElement>>("_elements");
        public Activity LoopAction { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            var selector = Selector.Get(context);
            var sel = new JavaSelector(selector);
            var timeout = TimeSpan.FromSeconds(3);
            var maxresults = MaxResults.Get(context);
            JavaElement[] elements = { };
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                elements = JavaSelector.GetElementsWithuiSelector(sel, null, maxresults);
            } while (elements.Count() > 0 && sw.Elapsed < timeout);

            context.SetValue(Elements, elements);
            IEnumerator<JavaElement> _enum = elements.ToList().GetEnumerator();
            context.SetValue(_elements, _enum);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                throw new Interfaces.ElementNotFoundException("Failed locating item");
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<JavaElement> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more)
            {
                context.ScheduleAction<JavaElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                if (LoopAction != null)
                {
                    context.ScheduleActivity(LoopAction, LoopActionComplete);
                }
            }
        }
        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Execute(context);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Selector", Selector);
            Interfaces.Extensions.AddCacheArgument(metadata, "From", From);
            Interfaces.Extensions.AddCacheArgument(metadata, "Elements", Elements);
            Interfaces.Extensions.AddCacheArgument(metadata, "MaxResults", MaxResults);
            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetElement();
            var aa = new ActivityAction<JavaElement>();
            var da = new DelegateInArgument<JavaElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
    }
}