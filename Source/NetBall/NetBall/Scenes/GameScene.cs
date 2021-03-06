﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using NetBall.Helpers;
using NetBall.Helpers.Network;
using NetBall.Helpers.Network.Messages;
using NetBall.GameObjects.Entities;
using NetBall.GameObjects.Props;

namespace NetBall.Scenes
{
    public class GameScene : ActionScene, EventListener
    {
        public Random generator { get; set; }
        public static GameScene instance;

        public GameScene(ContentManager content) : base()
        {
            instance = this;
            MessageUtils.initialize();

            //fetch the network data from the configure file
            FileData data = StartupUtils.readfileData();

            //determine if this computer is a client or a server and connect them
            if (data.isHost)
            {
                NetworkServer netServ = new NetworkServer(data.peer, data.port);
                netServ.startServer();
                GameSettings.IS_HOST = true;
            }
            else
            {
                NetworkClient netClient = new NetworkClient(data.peer, data.port);
                netClient.connectClient();
                GameSettings.IS_HOST = false;
            }

            // Wait until the connection to the other computer has been made
            while (!GameSettings.CONNECTED);

            generator = new Random();

            if (GameSettings.IS_HOST)
            {
                // Set up the initial round
                Vector2 ballPos = getBallStartPosition();

                NetworkServer.instance.sendData(MessageUtils.constructMessage(MessageType.BALL_SETUP,
                    new MessageDataBallSetup(ballPos)));

                addEntity(new Ball(content, ballPos));
            }
            else
            {
                MessageUtils.registerListener(this, MessageType.BALL_SETUP);
                GameSettings.SCREEN_OFFSET.X = -ScreenHelper.SCREEN_SIZE.X;
            }

            MessageUtils.registerListener(this, MessageType.GOAL);
            initialize(content);
        }

        private void initialize(ContentManager content)
        {
            // Background
            if (GameSettings.IS_HOST)
                addDeco(new Prop(content.Load<Texture2D>("Sprites/BackgroundLeft"), Vector2.Zero, 1.0f, false));
            else
                addDeco(new Prop(content.Load<Texture2D>("Sprites/BackgroundRight"), Vector2.Zero, 1.0f, false));


            int numBlocks = (int)(ScreenHelper.SCREEN_SIZE.X * 2) / 64;

            // Floor
            for (int i = 0; i < numBlocks; i++)
            {
                Block b = new Block(content, new Vector2(i * 64, ScreenHelper.SCREEN_SIZE.Y - 64));
                addEntity(b);
                groundList.Insert(b);

                b = new Block(content, new Vector2(i * 64, 0));
                addEntity(b);
                groundList.Insert(b);
            }

            // Walls
            for (int i = 0; i < 20; i++)
            {
                Block b = new Block(content, new Vector2(-64, GameSettings.SCREEN_OFFSET.Y + i * 64));
                addEntity(b);
                groundList.Insert(b);

                b = new Block(content, new Vector2(ScreenHelper.SCREEN_SIZE.X * 2, GameSettings.SCREEN_OFFSET.Y + i * 64));
                addEntity(b);
                groundList.Insert(b);
            }

            // Hoops
            Hoop h = new Hoop(content, GameSettings.HOOP_POSITION, true); 
            addEntity(h);
            groundList.Insert(h);

            h = new Hoop(content, new Vector2(ScreenHelper.SCREEN_SIZE.X * 2 - GameSettings.HOOP_POSITION.X, GameSettings.HOOP_POSITION.Y), false);
            addEntity(h);
            groundList.Insert(h);
        }

       
        public override void update(GameTime gameTime)
        {
            base.update(gameTime);

            // Update all the scene's entities
            foreach (Entity e in entityList)
            {
                e.update(gameTime);
            }

            foreach (Entity e in entityHUDList)
            {
                e.update(gameTime);
            }
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            BaseGame.instance.GraphicsDevice.Clear(Color.Purple);

            // In-game entities
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.PointClamp);

            foreach (Prop p in propList)
            {
                p.draw(spriteBatch);
            }

            foreach (Entity e in entityList)
            {
                e.draw(spriteBatch);
            }

            spriteBatch.End();

            // HUD
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.PointClamp);

            foreach (Entity e in entityHUDList)
            {
                e.draw(spriteBatch);
            }

            spriteBatch.End();
        }

        public Vector2 getBallStartPosition()
        {
            bool ballSide = generator.Next(2) == 0 ? true : false;

            Vector2 ballPos;
            
            if (ballSide)
                ballPos = new Vector2(ScreenHelper.SCREEN_SIZE.X + GameSettings.BALL_OFFSET.X, GameSettings.BALL_OFFSET.Y);
            else
                ballPos = new Vector2(ScreenHelper.SCREEN_SIZE.X - GameSettings.BALL_OFFSET.X, GameSettings.BALL_OFFSET.Y);

            return ballPos;
        }

        public void eventTriggered(MessageData data)
        {
            if (data.GetType() == typeof(MessageDataBallSetup))
            {
                MessageDataBallSetup castData = (MessageDataBallSetup)data;

                if (!GameSettings.IS_HOST)
                {
                    Entity b = getEntity(typeof(Ball));

                    if (b != null)
                    {
                        // Create score confetti
                        if (!GameSettings.IS_HOST)
                        {
                            for (int i = 0; i < 60; i++)
                            {
                                addEntity(new Confetti(SceneManager.content, new Vector2(ScreenHelper.SCREEN_SIZE.X * 2 - GameSettings.HOOP_POSITION.X, GameSettings.HOOP_POSITION.Y + 64)));
                            }
                        }

                        removeEntity(b);
                    }
                }

                addEntity(new Ball(SceneManager.content, castData.position));
            }
            else
            {
                // Create score confetti
                for (int i = 0; i < 60; i++)
                {
                    addEntity(new Confetti(SceneManager.content, new Vector2(ScreenHelper.SCREEN_SIZE.X * 2 - GameSettings.HOOP_POSITION.X, GameSettings.HOOP_POSITION.Y + 64)));
                }
            }
        }
    }
}
