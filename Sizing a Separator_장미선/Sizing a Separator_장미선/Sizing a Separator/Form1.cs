using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sizing_a_Separator
{
    public partial class Form1 : Form
    {
        //전역변수
        int ii;

        double P;
        double T;
        double API;
        double Z;
        double μo;
        double μg;
        double tro;
        double trw;
        double Qo;
        double Qw;
        double Qg;
        double SGo;
        double SGw;
        double SGg;
        double dmo;
        double dmw;
        double dmg;
        double ρg;
        double ρl;


        public Form1()
        {
            InitializeComponent();
        }

        private void GetTextBox()
        {
            P = Convert.ToDouble(textP.Text);
            T = Convert.ToDouble(textT.Text);
            API = Convert.ToDouble(textAPI.Text);
            Z = Convert.ToDouble(textZ.Text);
            μo = Convert.ToDouble(textμo.Text);
            μg = Convert.ToDouble(textμg.Text);
            tro = Convert.ToDouble(texttro.Text);
            trw = Convert.ToDouble(texttrw.Text);
            Qo = Convert.ToDouble(textQo.Text);
            Qw = Convert.ToDouble(textQw.Text);
            Qg = Convert.ToDouble(textQg.Text);
            SGo = Convert.ToDouble(textSGo.Text);
            SGw = Convert.ToDouble(textSGw.Text);
            SGg = Convert.ToDouble(textSGg.Text);
            dmo = Convert.ToDouble(textdmo.Text);
            dmw = Convert.ToDouble(textdmw.Text);
            dmg = Convert.ToDouble(textdmg.Text);
        }

        private void btn_ExGiven_Click(object sender, EventArgs e)
        {
            textP.Text = "100";
            textT.Text = "550";
            textAPI.Text = "30";
            textZ.Text = "0.84";
            textμo.Text = "10";
            textμg.Text = "0.013";
            texttro.Text = "10";
            texttrw.Text = "10";
            textQo.Text = "5000";
            textQw.Text = "3000";
            textQg.Text = "5";
            textSGo.Text = "0.876";
            textSGw.Text = "1.07";
            textSGg.Text = "0.6";
            textdmo.Text = "500";
            textdmw.Text = "200";
            textdmg.Text = "100";
        }

        private void btn_VerCal_Click(object sender, EventArgs e)
        {
            GetTextBox();

            //1. Calculate difference in specific gravities.
            //∆S.G.=(S.G.)_w-(S.G.)_o
            double SG = SGw - SGo;

            //2.Calculate d(Gas)
            //C_D iterate calculation
            double CD = IterateCal_GetCD();

            //결정된 C_D값으로 d 구하기
            //d^2=5040*(T*Z*Q_g/P)*(ρ_g/(ρ_l-ρ_g)*C_D/(d_m)_g)^(1/2)
            //d=sqrt(5040*(T*Z*Q_g/P)*(ρ_g/(ρ_l-ρ_g)*C_D/(d_m)_g)^(1/2))
            double dg = Math.Sqrt(5040 * (T * Z * Qg / P) * Math.Sqrt((ρg / (ρl - ρg) * CD / dmg)));
            
            //3.Calculate minimum diameter for water droplet settling.(Liquid)
            //d^2=6690*Q_o*μ_o/((∆S.G.)*((d_m)_o^2))
            //d=sqrt(6690*Q_o*μ_o/((∆S.G.)*((d_m)_o^2)))
            double dl = Math.Sqrt(6690 * Qo * μo / (SG * Math.Pow(dmo, 2)));

            //2,3에서 구한 d값을 서로 비교하여 큰 값을 d의 최소값으로 정한다.
            double dmin;
            if (dg <= dl)
                dmin = dl;
            else
                dmin = dg;
            
            //table에 들어갈 값 생성
            //d가 dmin~dratio(ratio > 1.5 에 있을때,d) 범위에 있는 동안 h_o+h_w,L_ss,slendernes ratio 구하기
            List<double> d = new List<double>();
            List<double> hoplushw = new List<double>();
            List<double> Lss = new List<double>();
            List<double> ratio = new List<double>();

            //dmin 부터 (처음)계산
            d.Add(dmin);

            //4. Liquid retention constraint.
            //h_o+h_w=((t_r)_o*Q_o+(t_r)_w*Qw)/(0.12*d^2)
            hoplushw.Add((tro * Qo + trw * Qw) / (0.12 * Math.Pow(d[0],2)));

            //5. Compute seam - to - seam length(Table5 - 1) as the larger of:
            //L_ss=(h_o+h_w+76)/12 or (h_o+h_w+d+40)/12
            Lss.Add((hoplushw[0] + d[0])/12);

            //6. Compute slenderness ratio. Choices in the range of 1.5 to 3 are common.
            //(12 * L_ss / d)
            ratio.Add(12 * Lss[0] / d[0]);
            
            int nextdmin = Convert.ToInt32(Math.Ceiling(dmin));
            for (ii = 1; ; ++ii)
            {
                d.Add((ii-1)+nextdmin);
                hoplushw.Add((tro * Qo + trw * Qw) / (0.12 * Math.Pow(d[ii], 2)));
                Lss.Add((hoplushw[ii] + d[ii]) / 12);
                ratio.Add(12 * Lss[ii] / d[ii]);

                if (ratio[ii] < 1.5)
                    break;
            }

            //DataGridView로 계산한 값 표시
            SetupDataGridView_Vertical(d, hoplushw, Lss, ratio);
        }

        private void btn_HoriCal_Click(object sender, EventArgs e)
        {
            //TextBox 안의 값 불러오기
            GetTextBox();

            //1. Calculate difference in specific gravities.
            //∆S.G.=(S.G.)_w-(S.G.)_o
            double SG = SGw - SGo;

            //2.Calculate d(Gas)
            //C_D iterate calculation
            double CD = IterateCal_GetCD();

            //3. 결정된 C_D값으로 d*L_eff 구할 수 있다. Liquid 상태와의 비교를 위해 임의의 d(60)값에서 Leff를 구한다.
            //d*L_eff=420*(T*Z*Q_g/P)*(ρ_g/(ρ_l-ρ_g)*C_D/(d_m)_g)^(1/2)
            //L_eff= 420 * (T * Z * Qg / P) * (ρ_g/(ρ_l-ρ_g)*C_D/(d_m)_g)^(1/2)/ d;
            double tempdg = 60;
            double Leffg = 420 * (T * Z * Qg / P) * Math.Sqrt((ρg / (ρl - ρg) * CD / dmg)) / tempdg;
            double dg = 420 * (T * Z * Qg / P) * Math.Sqrt((ρg / (ρl - ρg) * CD / dmg));

            //4. Calculate (h_o)_max
            //(h_o)_max=0.00128*(t_r)_o*(∆S.G.)*(d_m)_o^2/μ_o
            double homax = 0.00128 * tro * SG * Math.Pow(dmo,2) / μo;

            //5. Calculate A_w/A
            //A_w/A=0.5*Q_w*(T_r)_w/(Q_o*(t_r)_o+Q_w*(t_r)_w)
            double AwA = 0.5 * Qw * trw / (Qo * tro + Qw * trw);

            //6.Determine β(Refer to Figure5 - 8)
            double β = 0.9472* Math.Pow(AwA,2) - 1.4736*AwA + 0.5;

            //7. Calculate d
            //d = (h_o)_max / β
            double dl = homax / β;
            //Gas 상태와의 비교를 위해 임의의 d(60)값에서 Leff를 구한다.
            //(dl^2)*L_effl=1.42*{Q_w*(t_r)_w+Q_o*(t_r)_o}
            //L_effl=1.42*{Q_w*(t_r)_w+Q_o*(t_r)_o}/(dl^2)
            double tempdl = 60;
            double Leffl = 1.42 * (Qw * trw + Qo * tro) / Math.Pow(tempdl, 2);

            //8. Leff값을 서로 비교하고, L_eff값이 더 큰 경우의 상태를 기준으로 d의 최대값으로 정한다.
            //dg*L_effg=420*(T*Z*Q_g/P)*(ρ_g/(ρ_l-ρ_g)*C_D/(d_m)_g)^(1/2)와
            //(dl^2)*L_effl=1.42*{Q_w*(t_r)_w+Q_o*(t_r)_o}를 이용한다.
            double dmax;
            if (Leffg <= Leffl)
                dmax = dl;
            else
                dmax = dg;
            
            //table에 들어갈 값 생성
            //d가 1~dmax 범위에 있는 동안 L_eff,L_ss,slendernes ratio 구하기
            List<double> d = new List<double>();
            List<double> Leff = new List<double>();
            List<double> Lss = new List<double>();
            List<double> ratio = new List<double>();
            
            for (ii = 1; ii < dmax; ++ii)
            {
                d.Add(ii);

                //9. Calculate Combination of L_eff
                //(d^2)*L_eff=1.42*{Q_w*(t_r)_w+Q_o*(t_r)_o}
                //L_eff=1.42*{Q_w*(t_r)_w+Q_o*(t_r)_o}/(d^2)
                Leff.Add(1.42 * (Qw * trw + Qo * tro) / Math.Pow(d[ii-1], 2));

                //10. Estimate L_ss
                //L_ss=L_eff/0.75(liquid)
                Lss.Add(Leff[ii-1] / 0.75);

                //11.Compute slenderness ratio. Choices in the range of 3 to 5 are common.
                //(12 * L_ss / d)
                ratio.Add(12 * Lss[ii - 1] / d[ii - 1]);
            }

            //dmax일 때까지 (마지막)계산
            d.Add(dmax);
            Leff.Add(1.42 * (Qw * trw + Qo * tro) / Math.Pow(d[ii-1], 2));
            Lss.Add(Leff[ii-1] / 0.75);
            ratio.Add(12 * Lss[ii-1] / d[ii-1]);

            //DataGridView로 계산한 값 표시
            SetupDataGridView_Horizontal(d, Leff, Lss, ratio);
        }

        private double IterateCal_GetCD()
        {
            //2. Calculate d(Gas)
            //ρ_g = 2.7 * (S_g * P) / (T * Z)
            ρg = 2.7 * (SGg * P) / (T * Z);
            //ρ_l = 62.4 * (141.5 / (131.5 + API))
            ρl = 62.4 * (141.5 / (131.5 + API));

            //C_D iterate calculation
            //Assume C_D=0.34
            double CD = 0.34;
            double Vt;
            double Re;
            double tempCD = CD;

            while (Math.Abs(tempCD - CD) <= 0.001)
            {
                tempCD = CD;
                //① Calculate V_t=0.0119*((ρ_l-ρ_g)/ρ_g*(d_m)_g/C_D)^(1/2)
                Vt = 0.0119 * Math.Sqrt((ρl - ρg) / ρg * dmg / CD);
                //② Calculate Re=0.0049*ρ_g*(d_m)_g*V_t/μ_g
                Re = 0.0049 * ρg * dmg * Vt / μg;
                //③ Calculate C_D=24/Re+3/(Re^(1/2))+0.34
                CD = 24 / Re + 3 / Math.Sqrt(Re) + 0.34;
            }

            return CD;
        }

        private void SetupDataGridView_Vertical(List<double> d, List<double> hoplushw, List<double> Lss, List<double> ratio)
        {
            //이전 기록 지움
            dataGridView1.Columns.Clear();

            //DataGridView의 열 갯수를 4개로 설정
            dataGridView1.ColumnCount = 4;

            //열의 제목을 지정
            dataGridView1.Columns[0].Name = "d";
            dataGridView1.Columns[1].Name = "h_o + h_w";
            dataGridView1.Columns[2].Name = "L_ss";
            dataGridView1.Columns[3].Name = "slenderness ratio";

            //d, L_eff, L_ss, slendernes ratio 행 삽입
            for (ii = 0; ii < d.Count; ++ii)
            {
                //소수점 셋째자리까지 표현
                string tempd = string.Format("{0:f3}", d[ii]);
                string temphoplushw = string.Format("{0:f3}", hoplushw[ii]);
                string tempLss = string.Format("{0:f3}", Lss[ii]);
                string tempratio = string.Format("{0:f3}", ratio[ii]);

                //행 삽입
                dataGridView1.Rows.Add(tempd, temphoplushw, tempLss, tempratio);

                //ratio가 3~5 사이인 부분만 배경색 입힘
                if (1.5 <= ratio[ii] && ratio[ii] <= 3)
                    dataGridView1.Rows[ii].DefaultCellStyle.BackColor = Color.Beige;
            }
        }

        private void SetupDataGridView_Horizontal(List<double> d, List<double> Leff, List<double> Lss, List<double> ratio)
        {
            //이전 기록 지움
            dataGridView1.Columns.Clear();

            //DataGridView의 열 갯수를 4개로 설정
            dataGridView1.ColumnCount = 4;

            //열의 제목을 지정
            dataGridView1.Columns[0].Name = "d";
            dataGridView1.Columns[1].Name = "L_eff";
            dataGridView1.Columns[2].Name = "L_ss";
            dataGridView1.Columns[3].Name = "slenderness ratio";

            //d, L_eff, L_ss, slendernes ratio 행 삽입
            for (ii = 0; ii < d.Count; ++ii)
            {
                //소수점 셋째자리까지 표현
                string tempd = string.Format("{0:f3}", d[ii]);
                string tempLeff = string.Format("{0:f3}", Leff[ii]);
                string tempLss = string.Format("{0:f3}", Lss[ii]);
                string tempratio = string.Format("{0:f3}", ratio[ii]);

                //행 삽입
                dataGridView1.Rows.Add(tempd, tempLeff, tempLss, tempratio);

                //ratio가 3~5 사이인 부분만 배경색 입힘
                if (3 <= ratio[ii] && ratio[ii] <= 5)
                    dataGridView1.Rows[ii].DefaultCellStyle.BackColor = Color.Beige;
            }
        }
    }
}
