﻿using System;
using System.Collections.Generic;

namespace LearnInterpreter
{
    /*
        Refer to MyScript.gram for grammar 
    */

    public class Interpreter
    {
        private Lexer lexer;
        private Token currentToken;

        private NodeVisitor nodeVisitor;
        private SymbolTableBuilder symbolTableBuilder;

        public Interpreter(Lexer lexer)
        {
            this.lexer = lexer;
            currentToken = lexer.NextToken();

            nodeVisitor = new NodeVisitor();
            symbolTableBuilder = new SymbolTableBuilder();
        }

        private void Eat(TokenType tokenType)
        {
            if (tokenType == currentToken.TokenType)
                currentToken = lexer.NextToken();
            else
                throw new Exception($"Expected {tokenType}, got {currentToken.TokenType}");
        }

        private Node Program()
        {
            if (currentToken.TokenType == TokenType.OpenBracket)
            {
                return Block();
            }
            else
            {
                return StatementList();
            }
        }

        private Block Block()
        {
            Eat(TokenType.OpenBracket);
            Statements statements = StatementList();
            Eat(TokenType.CloseBracket);

            return new Block(statements);
        }

        private Statements StatementList()
        {
            Node node = Statement();

            List<Node> nodes = new List<Node>() { node };

            while (currentToken.TokenType == TokenType.Semicolon)
            {
                Eat(TokenType.Semicolon);
                nodes.Add(Statement());
            }

            return new Statements(nodes);
        }

        private Node Statement()
        {
            switch (currentToken.TokenType)
            {
                case TokenType.OpenBracket:
                    return Block();
                case TokenType.Identifier:
                    return AssignmentStatement();
                case TokenType.Type:
                    return VariableDeclarationStatement();
                case TokenType.Void:
                    return MethodDeclarationStatement();
                default:
                    return new NoOp();
            }
        }

        private VariableDeclaration VariableDeclarationStatement()
        {
            TypeNode type = TypeSpec();
            Variable var = Variable();

            return new VariableDeclaration(type, var);
        }

        private MethodDeclaration MethodDeclarationStatement()
        {
            Eat(TokenType.Void);

            Token token = currentToken;
            Eat(TokenType.Identifier);

            Eat(TokenType.LeftParen);
            Eat(TokenType.RightParen);

            Block block = Block();

            return new MethodDeclaration(token.Value, block);
        }

        private TypeNode TypeSpec()
        {
            Token type = currentToken;
            Eat(TokenType.Type);

            return new TypeNode(type);
        }

        private Node AssignmentStatement()
        {
            Variable left = Variable();
            Token op = currentToken;
            Eat(TokenType.Assign);
            Node right = Expr();

            return new Assign(op, left, right);
        }

        private Variable Variable()
        {
            Variable var = new Variable(currentToken);
            Eat(TokenType.Identifier);

            return var;
        }

        private Node Factor()
        {
            Token token = currentToken;

            if (currentToken.TokenType == TokenType.LeftParen)
            {
                Eat(TokenType.LeftParen);
                Node node = Expr();
                Eat(TokenType.RightParen);

                return node;
            }

            if (currentToken.TokenType == TokenType.Integer)
            {
                Node node = Number();
                return node;
            }

            if (currentToken.TokenType == TokenType.Plus || currentToken.TokenType == TokenType.Minus)
            {
                Eat(currentToken.TokenType);
                return new UnaryOp(token, Factor());
            }

            Node var = Variable();
            return var;
        }

        private Node Number()
        {
            Token token = currentToken;
            Eat(TokenType.Integer);

            string num = token.Value;

            if (currentToken.TokenType == TokenType.Dot)
            {
                Eat(TokenType.Dot);

                Token dec = currentToken;
                Eat(TokenType.Integer);

                num += $".{dec.Value}";
            }

            return new Number(num);
        }

        private Node Term()
        {
            Node node = Factor();

            while (currentToken.TokenType == TokenType.Mult || currentToken.TokenType == TokenType.Div)
            {
                Token token = currentToken;
                if (token.TokenType == TokenType.Mult)
                {
                    Eat(TokenType.Mult);
                }

                if (token.TokenType == TokenType.Div)
                {
                    Eat(TokenType.Div);
                }

                node = new BinOp(token, node, Factor());
            }

            return node;
        }

        private Node Expr()
        {
            Node node = Term();

            while (currentToken.TokenType == TokenType.Plus || currentToken.TokenType == TokenType.Minus)
            {
                Token token = currentToken;
                if (token.TokenType == TokenType.Plus)
                {
                    Eat(TokenType.Plus);
                }

                if (token.TokenType == TokenType.Minus)
                {
                    Eat(TokenType.Minus);
                }

                node = new BinOp(token, node, Term());
            }

            return node;
        }

        public Node Parse()
        {
            Node node = Program();
            if (currentToken.TokenType != TokenType.Eof)
                throw new Exception("Eof expected!");

            return node;
        }

        public void Evaluate()
        {
            Node ast = Parse();
            symbolTableBuilder.Visit(ast);
            nodeVisitor.Visit(ast);
        }
    }
}
