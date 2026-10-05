using PrescriptionLens.Editor;
int passed = 0;
void Check(bool condition, string label) {
 if (!condition) throw new Exception(label);
 passed++; Console.WriteLine("PASS " + label);
}
double n = 1.6, front = 4, d = .0015, r = .025;
double Thickness(double power, double angle = 0, double cyl = 0, double axis = 0) =>
 d + LensOpticsCalculator.BackSag(r*Math.Cos(angle), r*Math.Sin(angle), power,cyl,axis,front,n,d)
 - LensOpticsCalculator.Sag(front/(n-1),r);
Check(Math.Abs(Thickness(0)-d)<.00002,"Plano almost uniform");
Check(Thickness(-3)>d,"Myopia thicker edge");
Check(Thickness(-10)>Thickness(-3),"High myopia thicker than -3D");
Check(Thickness(5)<d,"Hyperopia thinner edge");
Check(Thickness(-3,Math.PI/2,-2)>Thickness(-3,0,-2),"CYL perpendicular meridian");
Check(Math.Abs(Thickness(-3,Math.PI/4,-2,Math.PI/4)-Thickness(-3,0,-2))<1e-12,"AXIS rotates 45 degrees");
foreach(double power in new[]{0d,-3,-10,5,-30}) {
 double c1=front/(n-1), c2=LensOpticsCalculator.BackCurvature(power,front,n,d);
 double recovered=(n-1)*(c1-c2+(n-1)*d*c1*c2/n);
 Check(Math.Abs(recovered-power)<1e-10,"Thick lens power "+power);
}
Check(LensOpticsCalculator.SampleScale(-3,.012)>1,"Negative power minifies image");
Check(LensOpticsCalculator.SampleScale(5,.012)<1,"Positive power magnifies image");
Check(LensOpticsCalculator.NormalizeAxis(-45)==135,"Axis normalization");
bool rejects=false;
try{LensOpticsCalculator.Sag(100,.025);}catch(ArgumentException){rejects=true;}
Check(rejects,"Invalid aperture rejected");
rejects=false;
try{LensOpticsCalculator.RequireFinite(double.NaN);}catch(ArgumentException){rejects=true;}
Check(rejects,"NaN rejected");
var fan=new List<int>();
for(int i=1;i<=32;i++)fan.AddRange(new[]{0,i,i==32?1:i+1});
var boundary=LensBoundaryBuilder.Build(fan,out int loops);
Check(boundary.Count==32 && loops==1,"Circle has one 32-edge boundary");
var bad=new List<int>(fan);bad.AddRange(new[]{0,1,2});
rejects=false;
try{LensBoundaryBuilder.Build(bad,out _);}catch(ArgumentException){rejects=true;}
Check(rejects,"Non-manifold source rejected");
Console.WriteLine($"{passed} checks passed.");
