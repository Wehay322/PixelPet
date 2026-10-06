using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

class Pet : AlphaWindow {
    Bitmap[,] frames = new Bitmap[4,6];
    Bitmap[] walkFrames = new Bitmap[8];
    System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();
    double previousTime,stepTime,animationTime,movementRemainder;
    int visitedWalk;
    Timer timer = new Timer(); Random random = new Random();
    int row=0, frame=0, ticks=0, direction=1, remaining=40, speed=3;
    bool dragging=false, paused=false; Point offset, down;
    NotifyIcon tray; Speech speech;
    static string folder=AppDomain.CurrentDomain.BaseDirectory;
    [STAThread] static void Main(string[] args) {
        Application.EnableVisualStyles();
        try { using(Pet pet=new Pet()) {
            if(args.Length>0 && args[0]=="--check") { Console.WriteLine("24 frames loaded; transparent sprites ready."); return; }
            if(args.Length>0 && args[0]=="--smoke") { Timer test=new Timer(); test.Interval=2400; test.Tick+=delegate{test.Stop();File.WriteAllText(Path.Combine(folder,"check.txt"),"24 frames loaded. Per-pixel alpha renderer ran. Speech shown and hidden: "+(!pet.speech.Visible));pet.Close();};test.Start(); }
            if(args.Length>0 && args[0]=="--walk-smoke") {pet.Left=Screen.PrimaryScreen.WorkingArea.Left+100;pet.direction=1;pet.Animate(1,60);int start=pet.Left;Timer test=new Timer();test.Interval=1150;test.Tick+=delegate{test.Stop();int distance=pet.Left-start;bool pass=pet.visitedWalk==255&&distance>65&&distance<105;File.WriteAllText(Path.Combine(folder,"walk-check.txt"),"Walking cycle: "+(pass?"PASS":"FAIL")+"; frames visited mask="+pet.visitedWalk+"; distance="+distance+" px; per-pixel alpha rendered.");pet.Close();};test.Start();}
            if(args.Length==0)pet.Animate(1,60);
            Application.Run(pet);
        }} catch(Exception e) { File.WriteAllText(Path.Combine(folder,"error.txt"),e.ToString()); MessageBox.Show(e.Message,"Питомец: ошибка"); }
    }
    Pet() {
        LoadFrames();LoadWalkFrames();
        FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; TopMost=true;
        ClientSize=new Size(160,160);
        StartPosition=FormStartPosition.Manual;
        Rectangle area=Screen.PrimaryScreen.WorkingArea; Location=new Point(area.Right-220,area.Bottom-160);
        ContextMenuStrip menu=new ContextMenuStrip();
        menu.Items.Add("О май гад!",null,delegate{Animate(0,24);});
        menu.Items.Add("Помахать",null,delegate{Animate(3,18);});
        menu.Items.Add("Ходить",null,delegate{paused=false;Animate(1,60);});
        menu.Items.Add("Бежать",null,delegate{paused=false;Animate(2,45);});
        menu.Items.Add("Пауза / продолжить",null,delegate{paused=!paused;});
        menu.Items.Add("Размер: маленький",null,delegate{ResizePet(128);});
        menu.Items.Add("Размер: обычный",null,delegate{ResizePet(160);});
        menu.Items.Add("Размер: большой",null,delegate{ResizePet(224);});
        menu.Items.Add("Выход",null,delegate{Close();}); ContextMenuStrip=menu;
        tray=new NotifyIcon(); tray.Icon=SystemIcons.Application; tray.Text="Пиксельный питомец"; tray.ContextMenuStrip=menu; tray.Visible=true;
        tray.DoubleClick+=delegate{paused=false;Show();Activate();};
        MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){dragging=true;offset=e.Location;down=Cursor.Position;Capture=true;}};
        MouseMove+=delegate{if(dragging){Point p=Cursor.Position;Location=new Point(p.X-offset.X,p.Y-offset.Y);}};
        MouseUp+=delegate(object sender,MouseEventArgs e){if(!dragging)return;dragging=false;Capture=false;Point p=Cursor.Position;if(Math.Abs(p.X-down.X)+Math.Abs(p.Y-down.Y)<6)Animate(3,18);Clamp();};
        speech=new Speech(this);
        timer.Interval=16;timer.Tick+=Tick;previousTime=clock.Elapsed.TotalSeconds;timer.Start();
        FormClosed+=delegate{timer.Stop();speech.Close();tray.Visible=false;tray.Dispose();foreach(Bitmap b in frames)b.Dispose();foreach(Bitmap b in walkFrames)if(b!=null)b.Dispose();};
    }
    void ResizePet(int size){int bottom=Bottom;ClientSize=new Size(size,size);Top=bottom-size;Clamp();RenderPet();}
    void Animate(int animation,int duration){row=animation;frame=0;ticks=0;remaining=duration;animationTime=0;stepTime=0;movementRemainder=0;RenderPet();}
    void Clamp(){Rectangle a=Screen.FromControl(this).WorkingArea;Left=Math.Max(a.Left,Math.Min(Left,a.Right-Width));Top=Math.Max(a.Top,Math.Min(Top,a.Bottom-Height));}
    void Tick(object sender,EventArgs e){
        double now=clock.Elapsed.TotalSeconds,dt=Math.Min(0.05,now-previousTime);previousTime=now;
        if(dragging||paused)return;
        animationTime+=dt;stepTime+=dt;
        while(stepTime>=0.1){stepTime-=0.1;ticks++;remaining--;if(row!=1)frame=(frame+1)%6;
            if(row==0)frame=ticks==1?1:ticks<10?2:ticks<18?3:ticks==18?4:0;
            if(row==0&&ticks==2)speech.Say();
        }
        if(row==1){frame=(int)(animationTime/0.125)%8;visitedWalk|=1<<frame;}
        if(row==1||row==2){Rectangle a=Screen.FromControl(this).WorkingArea;speed=row==2?140:80;movementRemainder+=speed*(Width/160.0)*dt;int pixels=(int)movementRemainder;movementRemainder-=pixels;Left+=direction*pixels;if(Left<a.Left){Left=a.Left;direction=1;}if(Right>a.Right){Left=a.Right-Width;direction=-1;}}
        if(remaining<=0){int choice=random.Next(10);direction=random.Next(2)==0?-1:1;Animate(choice<5?0:choice<8?1:choice==8?2:3,random.Next(24,65));}
        RenderPet();
    }
    protected override void OnShown(EventArgs e){base.OnShown(e);RenderPet();}
    void RenderPet(){
        if(!IsHandleCreated)return;
        using(Bitmap canvas=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb)){
            using(Graphics g=Graphics.FromImage(canvas)){
                g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
                Bitmap b=row==1?walkFrames[frame]:frames[row,frame];
                if(direction<0&&(row==1||row==2)){g.TranslateTransform(Width,0);g.ScaleTransform(-1,1);}
                g.DrawImage(b,new Rectangle(0,0,Width,Height),0,0,b.Width,b.Height,GraphicsUnit.Pixel);
            }
            Publish(canvas,255);
        }
    }
    void LoadWalkFrames(){
        using(Bitmap source=new Bitmap(Path.Combine(folder,"walk.png"))){
            int cw=source.Width/4,ch=source.Height/2;
            using(Bitmap preview=new Bitmap(cw*4,ch*2,PixelFormat.Format32bppArgb)){
                for(int i=0;i<8;i++){
                    using(Bitmap raw=source.Clone(new Rectangle(i%4*cw,i/4*ch,cw,ch),PixelFormat.Format32bppArgb)){
                        RemoveBackground(raw);
                        // Anchor the head and ground line to prevent cell-to-cell jitter.
                        int minY=ch,maxY=0;double headX=0;int count=0;
                        for(int y=0;y<ch;y++)for(int x=0;x<cw;x++){Color p=raw.GetPixel(x,y);if(p.A<128)continue;minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);if(y<ch*0.48&&p.R>140&&p.G>80&&p.B<190){headX+=x;count++;}}
                        double scale=230.0/Math.Max(1,maxY-minY+1);
                        float dx=(float)(151-(count>0?headX/count:cw/2)*scale),dy=(float)(249-(maxY+1)*scale);
                        Bitmap stable=new Bitmap(256,256,PixelFormat.Format32bppArgb);
                        using(Graphics g=Graphics.FromImage(stable)){g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(raw,new RectangleF(dx,dy,(float)(cw*scale),(float)(ch*scale)));}
                        walkFrames[i]=stable;
                        using(Graphics g=Graphics.FromImage(preview))g.DrawImage(stable,new Rectangle(i%4*cw,i/4*ch,cw,ch));
                    }
                }
                preview.Save(Path.Combine(folder,"walk-transparent.png"),ImageFormat.Png);
            }
        }
    }
    static void RemoveBackground(Bitmap b){
        int cw=b.Width,ch=b.Height;
        if(b.GetPixel(0,0).A==0&&b.GetPixel(cw-1,ch-1).A==0)return;
        bool[] seen=new bool[cw*ch],barrier=new bool[cw*ch];Queue<int> q=new Queue<int>();
        for(int yy=0;yy<ch;yy++)for(int xx=0;xx<cw;xx++){Color edge=b.GetPixel(xx,yy);if(edge.A>0&&edge.R<40&&edge.G<35&&edge.B<30)for(int dy=-5;dy<=5;dy++)for(int dx=-5;dx<=5;dx++){int bx=xx+dx,by=yy+dy;if(bx>=0&&bx<cw&&by>=0&&by<ch)barrier[by*cw+bx]=true;}}
        for(int x=0;x<cw;x++){q.Enqueue(x);q.Enqueue((ch-1)*cw+x);}for(int y=0;y<ch;y++){q.Enqueue(y*cw);q.Enqueue(y*cw+cw-1);}
        while(q.Count>0){int n=q.Dequeue();if(seen[n])continue;seen[n]=true;if(barrier[n])continue;int x=n%cw,y=n/cw;b.SetPixel(x,y,Color.Transparent);if(x>0)q.Enqueue(n-1);if(x<cw-1)q.Enqueue(n+1);if(y>0)q.Enqueue(n-cw);if(y<ch-1)q.Enqueue(n+cw);}
    }
    void LoadFrames(){
        using(Bitmap source=new Bitmap(Path.Combine(folder,"sprites.png"))){
            int cw=source.Width/6,ch=source.Height/4;
            using(Bitmap transparent=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb)){
                using(Graphics g=Graphics.FromImage(transparent))g.DrawImageUnscaled(source,0,0);
                for(int r=0;r<4;r++)for(int c=0;c<6;c++){
                    Bitmap b=source.Clone(new Rectangle(c*cw,r*ch,cw,ch),PixelFormat.Format32bppArgb);
                    // Background regions are bounded by the sprite's dark pixel outline.
                    bool[] seen=new bool[cw*ch],barrier=new bool[cw*ch];Queue<int> q=new Queue<int>();
                    for(int yy=0;yy<ch;yy++)for(int xx=0;xx<cw;xx++){Color edge=b.GetPixel(xx,yy);if(edge.A>0&&edge.R<40&&edge.G<35&&edge.B<30)for(int dy=-5;dy<=5;dy++)for(int dx=-5;dx<=5;dx++){int bx=xx+dx,by=yy+dy;if(bx>=0&&bx<cw&&by>=0&&by<ch)barrier[by*cw+bx]=true;}}
                    for(int x=0;x<cw;x++){q.Enqueue(x);q.Enqueue((ch-1)*cw+x);}
                    for(int y=0;y<ch;y++){q.Enqueue(y*cw);q.Enqueue(y*cw+cw-1);}
                    while(q.Count>0){int n=q.Dequeue();if(seen[n])continue;seen[n]=true;int x=n%cw,y=n/cw;Color p=b.GetPixel(x,y);
                        if(barrier[n])continue;
                        b.SetPixel(x,y,Color.Transparent);
                        if(x>0)q.Enqueue(n-1);if(x<cw-1)q.Enqueue(n+1);if(y>0)q.Enqueue(n-cw);if(y<ch-1)q.Enqueue(n+cw);
                    }
                    frames[r,c]=b;
                    using(Graphics g=Graphics.FromImage(transparent)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(b,c*cw,r*ch);}
                }
                transparent.Save(Path.Combine(folder,"sprites-transparent.png"),ImageFormat.Png);
            }
        }
    }
}

class Speech : AlphaWindow {
    Form pet;Timer fade=new Timer();DateTime started;byte alpha=255;
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override CreateParams CreateParams {get{CreateParams p=base.CreateParams;p.ExStyle|=0x20|0x08000000;return p;}}
    public Speech(Form owner){pet=owner;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;ClientSize=new Size(190,46);
        fade.Interval=30;fade.Tick+=delegate{double ms=(DateTime.UtcNow-started).TotalMilliseconds;Location=new Point(pet.Left+(pet.Width-Width)/2,Math.Max(Screen.FromControl(pet).WorkingArea.Top,pet.Top-Height));if(ms>=2000){fade.Stop();Hide();}else {alpha=(byte)(ms<1500?255:Math.Max(0,255*(1-(ms-1500)/500)));RenderSpeech();}};}
    public void Say(){started=DateTime.UtcNow;alpha=255;Location=new Point(pet.Left+(pet.Width-Width)/2,Math.Max(Screen.FromControl(pet).WorkingArea.Top,pet.Top-Height));Show(pet);RenderSpeech();fade.Start();}
    void RenderSpeech(){using(Bitmap canvas=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb)){using(Graphics g=Graphics.FromImage(canvas))using(Font f=new Font("Segoe UI",18,FontStyle.Bold)){g.Clear(Color.Transparent);string text="о май гад";SizeF s=g.MeasureString(text,f);float x=(Width-s.Width)/2,y=6;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++)g.DrawString(text,f,Brushes.Black,x+dx,y+dy);g.DrawString(text,f,Brushes.White,x,y);}Publish(canvas,alpha);}}
    protected override void Dispose(bool disposing){if(disposing)fade.Dispose();base.Dispose(disposing);}
}

class AlphaWindow : Form {
    [StructLayout(LayoutKind.Sequential)] struct NativePoint {public int X,Y;public NativePoint(int x,int y){X=x;Y=y;}}
    [StructLayout(LayoutKind.Sequential)] struct NativeSize {public int Width,Height;public NativeSize(int w,int h){Width=w;Height=h;}}
    [StructLayout(LayoutKind.Sequential,Pack=1)] struct Blend {public byte Operation,Flags,Alpha,Format;}
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("user32.dll",SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dst,ref NativePoint position,ref NativeSize size,IntPtr src,ref NativePoint origin,int key,ref Blend blend,int flags);
    protected override CreateParams CreateParams {get{CreateParams p=base.CreateParams;p.ExStyle|=0x80000;return p;}}
    protected override void OnPaintBackground(PaintEventArgs e){}
    protected override void OnPaint(PaintEventArgs e){}
    protected void Publish(Bitmap bitmap,byte opacity){
        if(!IsHandleCreated)return;
        IntPtr dc=GetDC(IntPtr.Zero),mem=CreateCompatibleDC(dc),image=IntPtr.Zero,old=IntPtr.Zero;
        try{image=bitmap.GetHbitmap(Color.FromArgb(0));old=SelectObject(mem,image);NativePoint pos=new NativePoint(Left,Top),origin=new NativePoint(0,0);NativeSize size=new NativeSize(bitmap.Width,bitmap.Height);Blend blend=new Blend();blend.Alpha=opacity;blend.Format=1;
            if(!UpdateLayeredWindow(Handle,dc,ref pos,ref size,mem,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }finally{if(old!=IntPtr.Zero)SelectObject(mem,old);if(image!=IntPtr.Zero)DeleteObject(image);DeleteDC(mem);ReleaseDC(IntPtr.Zero,dc);}
    }
}

