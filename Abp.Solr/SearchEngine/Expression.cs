using System;
using System.Collections.Generic;
using System.Text;

namespace Abp.Solr
{
    public class Expression
    {
        private Expression leftNode;
        private Expression rightNode;
        private FilterBase nodeData;
        private Operation operation;
        public Expression LeftNode
        {
            get
            {
                return this.leftNode;
            }
        }
        public Expression RightNode
        {
            get
            {
                return this.rightNode;
            }
        }
        public FilterBase NodeData
        {
            get
            {
                return this.nodeData;
            }
        }
        public Operation Operation
        {
            get
            {
                return this.operation;
            }
        }
        public Expression()
        {
        }
        public Expression(FilterBase data)
        {
            this.nodeData = data;
            this.operation = Operation.None;
        }
        public Expression(FilterBase data, Operation op)
        {
            if (op != Operation.NOT && op != Operation.None)
            {
                throw new System.Exception("运算符错误！叶节点只能使用单目运算符。");
            }
            this.nodeData = data;
            this.operation = op;
        }
        public Expression(Expression left, Expression right, Operation op)
        {
            if (op != Operation.OR && op != Operation.AND)
            {
                throw new System.Exception("运算符错误！此处只能使用双目运算符");
            }
            this.leftNode = left;
            this.rightNode = right;
            this.operation = op;
        }
        public Expression(FilterBase leftData, Expression right, Operation op)
        {
            if (op != Operation.OR && op != Operation.AND)
            {
                throw new System.Exception("运算符错误！此处只能使用双目运算符");
            }
            this.leftNode = new Expression(leftData);
            this.rightNode = right;
            this.operation = op;
        }
        public Expression(Expression left, FilterBase rightData, Operation op)
        {
            if (op != Operation.OR && op != Operation.AND)
            {
                throw new System.Exception("运算符错误！此处只能使用双目运算符");
            }
            this.leftNode = left;
            this.rightNode = new Expression(rightData);
            this.operation = op;
        }
        public Expression(FilterBase leftData, FilterBase rightData, Operation op)
        {
            if (op != Operation.OR && op != Operation.AND)
            {
                throw new System.Exception("运算符错误！此处只能使用双目运算符");
            }
            this.leftNode = new Expression(leftData);
            this.rightNode = new Expression(rightData);
            this.operation = op;
        }
        public bool HasChild()
        {
            return this.leftNode != null || this.rightNode != null;
        }
        public bool IsEmpty()
        {
            return this.leftNode == null && this.rightNode == null && this.nodeData == null;
        }
    }

    public enum Operation
    {
        None,
        AND,
        OR,
        NOT
    }
}
