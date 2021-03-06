/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 03/05/2008
 * Time: 11:16 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Data;
using System.Reflection;
using System.Text;

using Comunes;
using BasesDatos;

namespace ModeladorSql
{
	public delegate bool Filtro(Campo c);
	public class Tabla:IConCampos{
		Lista<Campo> campos;
		public string NombreTabla;
		public string Alias; // el que tiene la tabla siempre o el que se asigna a mano cuando hay una tabla que va a aparecer dos veces. El ejecutador no lo toca
		public string AliasActual; // el que decide el ejecutador si fuera necesario
		public BaseDatos db;
		public int CantidadCamposPk;
		public bool IniciadasFk=false;
		public Tabla TablaRelacionada;
		public ConjuntoTablas OtrasTablasRelacionadas;
		public Lista<Tabla> TablasFk;
		public Diccionario<string, Campo> CamposFkAlias=new Diccionario<string, Campo>();
		public Diccionario<Campo, IExpresion> CamposRelacionFk=new Diccionario<Campo, IExpresion>();
		public Fk.Tipo TipoFk=Fk.Tipo.Obligatoria;
		public bool LiberadaDelContextoDelEjecutador; // Del contexto del ejecutador
		public ListaCampos CamposContexto; // Para agregar en las clausulas where
		public bool RegistroConDatos=false;
		public SentenciaSelect SentenciaSubSelect;
		// public SelectInterno SelectInterno;
		public Tabla()
		{
			NombreTabla=this.GetType().Name.ToLowerInvariant();
			Construir();
		}
		public Tabla(BaseDatos db,params object[] Claves)
			:this()
		{
			Leer(db,Claves);
		}
		public bool ContieneMismoNombre(Campo c){
			return campos.Exists(
				delegate(Campo contenido){ 
					return contenido.NombreCampo==c.NombreCampo; 
				}
			);
		}
		public Lista<Campo> Campos(){
			return campos;
		}
		public virtual string ToSql(BaseDatos db){
			return (SentenciaSubSelect==null || SentenciaSubSelect.EsVistaExistente
			        ?db.StuffTabla(NombreTabla)
		            :"("+SentenciaSubSelect.ToSql(db)+")"
			       )
				+(AliasActual==null?"":" "+AliasActual);
		}
		public ConjuntoTablas Tablas(QueTablas queTablas){
			var rta=new ConjuntoTablas(this);
			if(SentenciaSubSelect!=null && queTablas==QueTablas.Aliasables){
				rta.AddRange(SentenciaSubSelect.Tablas(queTablas));
			}
			return rta;
		}
		public static string NombreFieldANombreCampo(string nombreField){
			return nombreField.Substring(1);
		}
		protected virtual void ConstruirCampos(){
			campos=new Lista<Campo>();
      		Assembly assem = Assembly.GetExecutingAssembly();
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
      				Campo c=(Campo)assem.CreateInstance(m.FieldType.FullName);
      				c.Nombre=NombreFieldANombreCampo(m.Name);
      				c.NombreCampo=c.Nombre.ToLowerInvariant();
      				c.TablaContenedora=this;
      				m.SetValue(this,c);
      				foreach (System.Attribute attr in m.GetCustomAttributes(true)){
      					if(attr is AplicadorCampo){
      						AplicadorCampo apl=attr as AplicadorCampo;
      						apl.Aplicar(ref c);
      					}
      					if(attr is Pk){
      						CantidadCamposPk++;
      					}
      					if(attr is Fk){
      						Fk fk=attr as Fk;
      						if(fk.Alias!=null){
      							if(fk.OrdenCampoAReemplazar==0){
	      							CamposFkAlias[fk.Alias]=c;
      							}else{
      								CampoDestino<object> ca=new CampoDestino<object>("");
      								ca.OrdenDeReemplazo=fk.OrdenCampoAReemplazar;
      								ca.Es(c);
	      							CamposFkAlias[fk.Alias]=ca;
      							}
      						}
      					}
      				}
   					campos.Add(c);
   				}
			}
		}
		protected void Construir(){
			foreach(System.Attribute attr in this.GetType().GetCustomAttributes(true)){
				if(attr is Alias){
					this.Alias=(attr as Alias).alias;
				}
			}
			ConstruirCampos();
		}
		public string SentenciaCreateTable(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			StringBuilder pk=new StringBuilder("\t"+"PRIMARY KEY (");
			rta.AppendLine("CREATE TABLE "+this.NombreTabla+"(");
      		Separador comapk=new Separador(",");
      		foreach(Campo c in campos){
				rta.AppendLine("\t"+c.NombreCampo+" "+c.TipoCampo+c.Opcionalidad+c.DefinicionPorDefecto(db)+",");
				if(c.EsPk){
					pk.Append(comapk+c.NombreCampo);
				}
			}
      		pk.Append(")");
      		rta.Append(pk);
      		UsarFk();
      		if(TablasFk!=null){
      			// System.Console.WriteLine("La tabla {0} tiene Fk",this.NombreTabla);
	      		foreach(Tabla t in TablasFk){
      				// System.Console.WriteLine("  tablaFk {0}",t.NombreTabla);
      				if(t.TipoFk==Fk.Tipo.Obligatoria || t.TipoFk!=Fk.Tipo.Sugerida && db.SoportaFkMixta){
      					// System.Console.Write("     Tipo {0}:",t.TipoFk.ToString());
		      			StringBuilder camposFkEsta=new StringBuilder();
		      			StringBuilder camposFkOtra=new StringBuilder();
		      			Separador coma=new Separador(",");
		      			foreach(System.Collections.Generic.KeyValuePair<Campo, IExpresion> p in t.CamposRelacionFk){
		      				// System.Console.Write(" [{0},{1}]",p.Key.NombreCampo,p.Value.ToString());
							Campo c=p.Value as Campo;		      				
		      				camposFkEsta.Append(coma+c.NombreCampo);
		      				camposFkOtra.Append(coma.mismo()+p.Key.NombreCampo);
		      			}
		      			// System.Console.WriteLine();
		      			rta.Append(",\n\t"+"FOREIGN KEY ("+camposFkEsta.ToString()+") REFERENCES "+t.NombreTabla+" ("+camposFkOtra.ToString()+")");
      				}
	      		}
      		}
			rta.AppendLine("\n);");
			return rta.ToString();
		}
		public virtual void InsertarValores(BaseDatos db,params IConCampos[] Campos){
			Sentencia s=new SentenciaInsert(this).Valores(Campos);
			Ejecutador ej=new Ejecutador(db);
			ej.Ejecutar(s);
		}
		public virtual Tabla InsertarDirecto(BaseDatos db,params object[] Valores){
			int i=0;
			using(Insertador ins=new Insertador(db,this)){
				foreach(Campo c in this.Campos()){
				if(i>=Valores.Length) break;
					c[ins]=Valores[i];
					i++;
				}
			}
			Leer(db,Valores);
			return this;
		}
		/*
		public void Insertar(BaseDatos db,params object[] Valores){
			using(InsertadorSql ins=new InsertadorSql(db,this.NombreTabla)){
      			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
      			int i=0;
				foreach(FieldInfo m in ms){
				if(i>=Valores.Length) break;
					if(m.FieldType.IsSubclassOf(typeof(Campo))){
						Campo c=(Campo)m.GetValue(this);
						ins[c.Nombre]=Valores[i];
					}
					i++;
      			}
      			// ins.InsertarSiHayCampos();
			}
		}
		*/
		public Insertador Insertar(BaseDatos db){
			return new Insertador(db,this);
		}
		public virtual void Leer(BaseDatos db,params object[] Codigos){
			BuscarYLeer(db,true,Codigos);
		}
		public virtual bool Buscar(BaseDatos db,params object[] Codigos){
			return BuscarYLeer(db,false,Codigos);
		}
		bool BuscarYLeer(BaseDatos db,bool LanzaExcepcion,params object[] Codigos){
			this.db=db;
			Lista<object> Valores=new Lista<object>();
			foreach(object o in Codigos){
				if(o is Campo){
					Campo c=o as Campo;
					if(c.ValorSinTipo!=null){
						Valores.Add(c);
					}else if(c is ICampoAlias && (c as ICampoAlias).ExpresionBase.CandidatoAGroupBy==false){
						Valores.Add((c as ICampoAlias).ExpresionBase);
					}else{
						Falla.Detener("BuscarYLeer un par�metro Campo ("+c.Nombre+") no tiene valor ni expresion base");
					}
				}else if(o is Tabla){
					Tabla t=o as Tabla;
					foreach(Campo c in t.CamposPk()){
						Valores.Add(c.ValorSinTipo);
					}
				}else{
					Valores.Add(o);
				}
			}
			int i=0;
			object[] parametros=new object[CantidadCamposPk*2];
			foreach(Campo c in CamposPk()){
			if(i>=Valores.Count) break;
				parametros[i*2]=c.NombreCampo;
				parametros[i*2+1]=Valores[i];
				i++;
			}
			if(i<CamposPk().Count){
				Falla.Detener("Faltaron especificar campos en BuscarYLeer de "+this.NombreTabla+" con "+Objeto.ExpandirMiembros(Codigos));
			}
			/*
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
			if(i>=Valores.Count) break;
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					if(c.EsPk){
						parametros[i*2]=c.NombreCampo;
						parametros[i*2+1]=Valores[i];
					}
				}
				i++;
  			}
  			*/
  			return BuscarYLeerNoPk(db,LanzaExcepcion,parametros);
		}
		public virtual void LeerNoPk(BaseDatos db,params object[] parametros){
			BuscarYLeerNoPk(db,true,parametros);
		}
		bool BuscarYLeerNoPk(BaseDatos db,bool LanzaExcepcion,params object[] parametros){
			this.db=db;
			Separador whereAnd=new Separador("\n WHERE ","\n AND ");
			StringBuilder clausulaWhere=new StringBuilder();
			for(int i=0;i<parametros.Length;i++){
				object campo=parametros[i];
				object valor=null;
				if(campo is Campo){
					Campo c=campo as Campo;
					campo=c.NombreCampo;
					if(c is ICampoAlias){
						valor=(c as ICampoAlias).ExpresionBase.ToSql(db);
					}else{
						valor=db.StuffValor(c.ValorSinTipo);
					}
				}else if(campo is string){
					i++;
					valor=parametros[i];
					if(valor is Campo){
						Campo c=valor as Campo;
						valor=db.StuffValor(c.ValorSinTipo);
					}else{
						valor=db.StuffValor(valor);
					}
				}else{
					Falla.Detener("El par�metro no tiene el tipo string o campo.Es "+Objeto.ExpandirMiembros(campo));
				}
				clausulaWhere.Append(whereAnd+campo+"="+valor);
  			}
			IDataReader SelectAbierto=db.ExecuteReader("SELECT * FROM "+db.StuffTabla(NombreTabla)+clausulaWhere+";");
			RegistroConDatos=SelectAbierto.Read();
			if(RegistroConDatos){
				LevantarCampos(SelectAbierto);
			}else{
				if(LanzaExcepcion){
					throw new SystemException("No existe el campo descripto por "+Objeto.ExpandirMiembros(parametros));
				}
			}
			return RegistroConDatos;
		}
		public void LevantarCampos(IDataReader SelectAbierto){
			RegistroConDatos=true;
			foreach(Campo c in Campos()){
				c.AsignarValor(SelectAbierto[c.NombreCampo]);
  			}
		}
		public virtual ListaCampos Campos(Filtro filtro){
			ListaCampos rta=new ListaCampos();
			foreach(Campo c in campos){
				if(filtro==null || filtro(c)){
					rta.Add(c);
				}
  			}
  			return rta;
		}
		public virtual ListaCampos CamposPk(){
			return Campos(delegate(Campo c){ return c.EsPk; });
		}
		public virtual string OrderBy(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			Separador coma=new Separador(", ");
			foreach(Campo c in CamposPk()){
				rta.Append(coma+c.ToSql(db));
			}
			return rta.ToString();
		}
		public virtual Campo CampoIndirecto(string campoNombre){
			return campos.Find(
				delegate(Campo c){
					return c.Nombre==campoNombre;
				}
			);
		}
		public virtual Campo CampoIndirecto(Campo campo){
			return CampoIndirecto(campo.Nombre);
		}
		public string MostrarCampos(){
			StringBuilder rta=new StringBuilder();
			Separador coma=new Separador(", ");
			foreach(Campo c in Campos()){
				rta.Append(coma+c.Nombre+"="+Cadena.DelValor(c.ValorSinTipo));
			}
			return rta.ToString();
		}
		public void EsFkDe(Tabla maestra,Fk.Tipo TipoFk,params IConCampos[] CamposReemplazo){
			this.TablaRelacionada=maestra;
			this.OtrasTablasRelacionadas=new ConjuntoTablas();
			int cantidadCamposFk=CamposPk().Count;
			ListaCampos CampoAReemplazar=new ListaCampos();
			ListaElementos<IExpresion> ExpresionDeReemplazo=new ListaElementos<IExpresion>();
			ListaCampos CampoASaltear=new ListaCampos();
			CamposRelacionFk=new Diccionario<Campo,IExpresion>();
			// Console.WriteLine("{0} EsFkDe: {1}",this,maestra);
			var OtrasTablas=new ConjuntoTablas();
			foreach(var t in CamposReemplazo){
				if(t==null){
				}else if(t is Campo){
					Campo CampoReemplazo=t as Campo;
					if(CampoReemplazo is ICampoDestino){
						CampoDestino<object> cd=CampoReemplazo as CampoDestino<object>;
						CampoAReemplazar.Add(CamposPk()[cd.OrdenDeReemplazo-1]);
						Campo r=cd.CampoContenedor.Expresion as Campo;
						Falla.SiEsNulo(r);
						ExpresionDeReemplazo.Add(r);
						OtrasTablasRelacionadas.Add(r.TablaContenedora);
					}else if(CampoReemplazo.TablaContenedora==maestra){
						CampoAReemplazar.Add(CamposPk()[cantidadCamposFk-1]);
						ExpresionDeReemplazo.Add(CampoReemplazo);
						OtrasTablasRelacionadas.Add(CampoReemplazo.TablaContenedora);
					}else if(CampoReemplazo is ICampoAlias){
						CampoAReemplazar.Add((CampoReemplazo as ICampoAlias).CampoReceptor);
						ExpresionDeReemplazo.Add((CampoReemplazo as ICampoAlias).ExpresionBase);
						OtrasTablasRelacionadas.AddRange((CampoReemplazo as ICampoAlias).ExpresionBase.Tablas(QueTablas.Aliasables));
					}else if(CampoReemplazo.TablaContenedora==this){ // No es un alias, quitar
						CampoASaltear.Add(CampoReemplazo);
					}
				}else{
					OtrasTablas.Add(t as Tabla);
					OtrasTablasRelacionadas.Add(t as Tabla);
					/*
					foreach(Campo CampoReemplazo in t.Campos()){
						var CampoReemplazado=CampoIndirecto(CampoReemplazo);
						if(!CampoAReemplazar.Contains(CampoReemplazado)){
							CampoAReemplazar.Add(CampoReemplazado);
							ExpresionDeReemplazo.Add(CampoReemplazo);
							OtrasTablasRelacionadas.Add(CampoReemplazo.TablaContenedora);
						}
					}
					Console.WriteLine("Paso: {0} {1}",CampoAReemplazar,ExpresionDeReemplazo);
					}
					*/
				}
			}
			foreach(Campo c in CamposPk()){
				if(!CampoASaltear.Contains(c)){
					if(CampoAReemplazar!=null && CampoAReemplazar.IndexOf(c)>=0){
						CamposRelacionFk[c]=ExpresionDeReemplazo[CampoAReemplazar.IndexOf(c)];
					}else{
						Campo CampoReemplazo=maestra.CampoIndirecto(c);
						if(CampoReemplazo==null){
							foreach(Tabla t in OtrasTablas.Keys){
								CampoReemplazo=t.CampoIndirecto(c);
								if(CampoReemplazo!=null) break;
							}
						}
						Falla.SiEsNulo(CampoReemplazo);
						CamposRelacionFk[c]=CampoReemplazo;
					}
				}
			}
			this.TipoFk=TipoFk;
		}
		public void EsFkDe(Tabla maestra){
			EsFkDe(maestra,Fk.Tipo.Sugerida,new Campo[]{});
		}
		public void EsFkDe(Tabla maestra,params IConCampos[] CampoReemplazo){
			EsFkDe(maestra,Fk.Tipo.Sugerida,CampoReemplazo);
		}
		public void NoEsFk(){
			TablaRelacionada=null;
		}
		public void UsarFk(){
			if(!IniciadasFk || RegistroConDatos){
				TablasFk=new Lista<Tabla>();
      			Assembly assem = Assembly.GetExecutingAssembly();
	  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
				foreach(FieldInfo m in ms){
					if(m.FieldType.IsSubclassOf(typeof(Tabla))){
	  					foreach (System.Attribute attr in m.GetCustomAttributes(true)){
	  						if(attr is Fk){
	  							Fk fk=attr as Fk;
	  							if(!IniciadasFk){
	      							Tabla nueva=(Tabla)assem.CreateInstance(m.FieldType.FullName);
	      							nueva.EsFkDe(this,fk.TipoFk,(fk.Alias!=null?this.CamposFkAlias[fk.Alias]:null));
	      							m.SetValue(this,nueva);
	      							TablasFk.Add(nueva);
	  							}
	  							if(RegistroConDatos){
	  								Tabla fkTabla=(Tabla) this.GetType().GetField(m.Name).GetValue(this);
	  								Lista<object> Campos=new Lista<object>();
	  								foreach(System.Collections.Generic.KeyValuePair<Campo,IExpresion> p in fkTabla.CamposRelacionFk){
	  									if(p.Value is Campo){
	  										Campos.Add(p.Value);
	  									}else{
	  										Campos.Add(p.Key.EsObject(p.Value));
	  									}
  										// Campos.Add(p.Key.EsObject(p.Value));
	  								}
	  								fkTabla.BuscarYLeerNoPk(db,false,Campos.ToArray());
	  							}
	  						}
	  					}
	  				}
	  			}
	  			IniciadasFk=true;
			}
		}
		public virtual bool CandidatoAGroupBy{ 
			get{ // No tiene en el sentido de que no es una expresi�n
				Falla.Detener("No deber�a preguntar si una tabla tiene variables");
				return false; 
			} 
		} 
		/*
		public ExpresionSql SelectSuma(Campo CampoSumar,ExpresionSql ExpresionWhere){
			return new ExpresionSql.SelectSuma(this,CampoSumar,ExpresionWhere);
		}
		*/
		public IElementoTipado<T> SelectSuma<T>(IElementoTipado<T> expresion){
			return new SubSelectAgrupado<T>(expresion,OperadorAgrupada.Suma,this);
		}
		public IElementoTipado<T> SelectPromedioGeometrico<T>(IElementoTipado<T> expresion){
			return new SubSelectAgrupado<T>(expresion,OperadorAgrupada.PromedioGeometrico,this,expresion.Mayor<T>(Constante<T>.Cero));
		}
		public IElementoTipado<T> SelectMin<T>(IElementoTipado<T> expresion){
			return new SubSelectAgrupado<T>(expresion,OperadorAgrupada.Minimo,this);
		}
		public RegistrosEnumerables Todos(BaseDatos db){
			return new RegistrosEnumerables(this,db);
		}
		public RegistrosEnumerables Algunos(BaseDatos db,ElementoTipado<bool> ClausulaWhere,params Campo[] CamposOrden){
			var clausula=new ElementosClausulaWhere();
			clausula.Add(ClausulaWhere);
			return new RegistrosEnumerables(this,db,clausula,CamposOrden);
		}
		public SentenciaSelect SubSelect(string AliasSubSelect,params IConCampos[] Campos){
			Alias=AliasSubSelect;
			SentenciaSubSelect=new SentenciaSelect(Campos);
			SentenciaSubSelect.EsInterno=true;
			SentenciaSubSelect.TablaParaRestringirCampos=this;
			// SelectInterno=new SelectInterno(SentenciaSubSelect);
			return SentenciaSubSelect;
		}
		public ElementoTipado<bool> NoExistePara(Tabla TablaContexto/*,params Tabla[] OtrasTablaContexto*/){
			Falla.Si(this.TablaRelacionada!=TablaContexto,"Para usar NoExistePara tiene que estar relacionada "+this.NombreTabla+" y "+TablaContexto.NombreTabla);
			var select=new SentenciaSelect(campos[0]);
			select.TablasQueEstanMasArriba.Add(TablaContexto);
			select.TablaBase=TablaContexto;
			return new ExpresionSubSelect{SubSelect=select};
		}
		public ElementoTipado<bool> NoExiste(){
			Falla.SiEsNulo(CamposFkAlias);
			var select=new SentenciaSelect(campos[0]);
			ConjuntoTablas contexto=new ConjuntoTablas();
			// System.Console.WriteLine("NoExiste: {0}",this.NombreTabla);
			foreach(var par in CamposRelacionFk){
				// System.Console.WriteLine("Campo relacionado Fk {0},{1}",par.Key.Nombre,par.Value.ToString());
				contexto.AddRange(par.Value.Tablas(QueTablas.Aliasables));
			}
			select.TablasQueEstanMasArriba.AddRange(contexto);
			// select.TablaBase=TablaContexto;
			return new ExpresionSubSelect{SubSelect=select};
		}
		/*
		public ExpresionSql NoExistePara(params Campable[] CamposOTablas){
			ListaSqlizable<Campo> Campos=new ListaSqlizable<Campo>();
			ListaSqlizable<ExpresionSql> ExpresionesWhere=new ListaSqlizable<ExpresionSql>();
			ConjuntoTablas TablasLibres=new ConjuntoTablas();
			foreach(Campable cc in CamposOTablas){
				foreach(Campo c in cc.Campos()){
					if(this.TieneElCampo(c) && this.CampoIndirecto(c).EsPk){
						ExpresionesWhere.Add(this.CampoIndirecto(c).Igual(c));
						if(c.TablaContenedora!=this){
							TablasLibres.Add(c.TablaContenedora);
						}
					}
				}
			}
			Sentencia s=new SentenciaSelect(this.CamposPk()[0])
				.Where(ExpresionesWhere.ToArray())
				.Libres(TablasLibres);
			return new ExpresionSql(new LiteralSql("NOT EXISTS ("),new SelectInterno(s),new LiteralSql(")"));
		}
		*/
		/*
		public ExpresionSql NotIn(Tabla t){
			ListaSqlizable<Sqlizable> Partes1=new ListaSqlizable<Sqlizable>();
			ListaSqlizable<Sqlizable> Partes2=new ListaSqlizable<Sqlizable>();
			ParteSeparadora p1=new ParteSeparadora("(",", ");
			ParteSeparadora p2=new ParteSeparadora(") NOT IN (SELECT ",",");
			foreach(Campo c in this.CamposPk()){
				if(t.TieneElCampo(c)){
					p1.AgregarEn(Partes1,c);
					p2.AgregarEn(Partes2,t.CampoIndirecto(c));
				}
			}
			if(Partes1.Count==0){
				Falla.Detener("Tiene que haber campos en el not in");
			}
			Partes1.AddRange(Partes2);
			Partes1.Add(new LiteralSql(" FROM "));
			Partes1.Add(new LiteralSql(t.NombreTabla));
			Partes1.Add(new LiteralSql(")"));
			return new ExpresionSql(Partes1);
		}
		*/
		public override string ToString()
		{
			return this.NombreTabla
				+"("+this.AliasActual
				+(this.IniciadasFk?" iFk":"")
				+(this.LiberadaDelContextoDelEjecutador?" LC":"")
				+(this.RegistroConDatos?" c/d":"")
				+(this.TablaRelacionada==null?""
				  :" tr:"+this.TablaRelacionada.NombreTabla
				    +(this.TablaRelacionada.AliasActual!=""?".":"")
				    +this.TablaRelacionada.AliasActual)+")";
		}
	}
	public class RegistrosEnumerables{
		BaseDatos db;
		Tabla TablaARecorrer;
		ElementosClausulaWhere ClausulaWhere;
		Campo[] CamposOrden;
		public RegistrosEnumerables(Tabla TablaARecorrer,BaseDatos db)
			:this(TablaARecorrer,db,null,new Campo[]{})
		{}
		public RegistrosEnumerables(Tabla TablaARecorrer,BaseDatos db,ElementosClausulaWhere ClausulaWhere,Campo[] CamposOrden){
			this.db=db;
			this.TablaARecorrer=TablaARecorrer;
			this.ClausulaWhere=ClausulaWhere;
			this.CamposOrden=CamposOrden;
		}
		public IteradorRegistro GetEnumerator(){
			return new IteradorRegistro(TablaARecorrer,db,ClausulaWhere,CamposOrden);
		}
	}
	public class IteradorRegistro{
		Tabla RegistroActual;
		IDataReader SelectAbierto;
		BaseDatos db;
		bool HayActual;
		string Sentencia;
		public IteradorRegistro(Tabla TablaBase,BaseDatos db,ElementosClausulaWhere ClausulaWhere,params Campo[] Campos){
			this.db=db;
			this.RegistroActual=TablaBase;
			StringBuilder s=new StringBuilder();
			s.Append("SELECT * FROM "+db.StuffTabla(TablaBase.NombreTabla));
			if(ClausulaWhere!=null){
				s.Append(" WHERE "+ClausulaWhere.ToSql(db));
			}
			if(Campos.Length==0){
				s.Append(" ORDER BY "+TablaBase.OrderBy(db));
			}else{
				Separador coma=new Separador(" ORDER BY ",", ");
				foreach(Campo c in Campos){
					s.Append(coma+c.ToSql(db)+c.DireccionOrderBy);
				}
			}
			s.Append(";");
			Sentencia=s.ToString();
			Reset();
		}
		public void Reset(){
			SelectAbierto=db.ExecuteReader(Sentencia);
			HayActual=false;
		}
		public bool MoveNext(){
			HayActual=SelectAbierto.Read();
			if(HayActual){
				RegistroActual.LevantarCampos(SelectAbierto);
			}
			return HayActual;
		}
		public Tabla Current{
			get{
				if(HayActual){
					return RegistroActual; 
				}else{
					throw new InvalidOperationException();
				}
			}
		}
		public void Dispose(){
			SelectAbierto.Close();
		}
	}
	/////////////
	public class Vista:System.Attribute{}
	public class Alias:System.Attribute{
		public string alias;
		public Alias(string alias){
			this.alias=alias;
		}
	}
	public class Fk:System.Attribute{		
		public enum Tipo { Obligatoria, Mixta/*puede tener alg�n campo null y otro no*/, Sugerida/*solo para los joins*/ };
		public Tipo TipoFk;
		public string Alias;
		public int OrdenCampoAReemplazar;
		public Fk(){}
		public Fk(string Alias):this(Alias,Tipo.Obligatoria){}
		protected Fk(string Alias,Tipo TipoFk){
			this.Alias=Alias;
			this.TipoFk=TipoFk;
		}
	}
	public class FkMixta:Fk{
		public FkMixta(string Alias)
			:base(Alias,Tipo.Mixta)
		{
		}
		public FkMixta(string Alias,int OrdenCampoAReemplazar)
			:base(Alias,Tipo.Mixta)
		{
			this.OrdenCampoAReemplazar=OrdenCampoAReemplazar;
		}
	}
	public class Insertador:InsertadorSql{
		public Insertador(BaseDatos db,Tabla tabla)
			:base(db,tabla.NombreTabla)
		{}
	}
}
