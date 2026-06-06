

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0628 */
/* at Tue Jan 19 04:14:07 2038
 */
/* Compiler settings for C:\Users\RHAEGA~1\AppData\Local\Temp\Converters.idl-235c4bfc:
    Oicf, W1, Zp8, env=Win64 (32b run), target_arch=AMD64 8.01.0628 
    protocol : all , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
/* @@MIDL_FILE_HEADING(  ) */



/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 500
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif /* __RPCNDR_H_VERSION__ */

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __Converters_h_p_h__
#define __Converters_h_p_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

#ifndef DECLSPEC_XFGVIRT
#if defined(_CONTROL_FLOW_GUARD_XFG)
#define DECLSPEC_XFGVIRT(base, func) __declspec(xfg_virtual(base, func))
#else
#define DECLSPEC_XFGVIRT(base, func)
#endif
#endif

#if defined(__cplusplus)
#if defined(__MIDL_USE_C_ENUM)
#define MIDL_ENUM enum
#else
#define MIDL_ENUM enum class
#endif
#endif


/* Forward Declarations */ 

#ifndef ____x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_FWD_DEFINED__
#define ____x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_FWD_DEFINED__
typedef interface __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics;

#endif 	/* ____x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_FWD_DEFINED__ */


/* header files for imported files */
#include "inspectable.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_Converters_0000_0000 */
/* [local] */ 




extern RPC_IF_HANDLE __MIDL_itf_Converters_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_Converters_0000_0000_v0_0_s_ifspec;

#ifndef ____x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_INTERFACE_DEFINED__
#define ____x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_INTERFACE_DEFINED__

/* interface __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics */
/* [object][uuid] */ 


EXTERN_C const IID IID___x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("54ba0c0e-a083-5619-9309-2fce36f7d1b3")
    __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics : public IInspectable
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE InvertBoolean( 
            /* [in] */ boolean value,
            /* [retval][out] */ boolean *result) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PercentageToPercentageValue( 
            /* [in] */ double value,
            /* [retval][out] */ double *result) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PercentageValueToPercentage( 
            /* [in] */ double value,
            /* [retval][out] */ double *result) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StringsAreNotEqual( 
            /* [in] */ HSTRING expected,
            /* [in] */ HSTRING actual,
            /* [retval][out] */ boolean *result) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StringNotEmpty( 
            /* [in] */ HSTRING value,
            /* [retval][out] */ boolean *result) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StringOrEmptyIfPlaceholder( 
            /* [in] */ HSTRING placeholder,
            /* [in] */ HSTRING value,
            /* [retval][out] */ HSTRING *result) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStaticsVtbl
    {
        BEGIN_INTERFACE
        
        DECLSPEC_XFGVIRT(IUnknown, QueryInterface)
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        DECLSPEC_XFGVIRT(IUnknown, AddRef)
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This);
        
        DECLSPEC_XFGVIRT(IUnknown, Release)
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This);
        
        DECLSPEC_XFGVIRT(IInspectable, GetIids)
        HRESULT ( STDMETHODCALLTYPE *GetIids )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This,
            /* [out] */ ULONG *iidCount,
            /* [size_is][size_is][out] */ IID **iids);
        
        DECLSPEC_XFGVIRT(IInspectable, GetRuntimeClassName)
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeClassName )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This,
            /* [out] */ HSTRING *className);
        
        DECLSPEC_XFGVIRT(IInspectable, GetTrustLevel)
        HRESULT ( STDMETHODCALLTYPE *GetTrustLevel )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This,
            /* [out] */ TrustLevel *trustLevel);
        
        DECLSPEC_XFGVIRT(__x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics, InvertBoolean)
        HRESULT ( STDMETHODCALLTYPE *InvertBoolean )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This,
            /* [in] */ boolean value,
            /* [retval][out] */ boolean *result);
        
        DECLSPEC_XFGVIRT(__x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics, PercentageToPercentageValue)
        HRESULT ( STDMETHODCALLTYPE *PercentageToPercentageValue )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This,
            /* [in] */ double value,
            /* [retval][out] */ double *result);
        
        DECLSPEC_XFGVIRT(__x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics, PercentageValueToPercentage)
        HRESULT ( STDMETHODCALLTYPE *PercentageValueToPercentage )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This,
            /* [in] */ double value,
            /* [retval][out] */ double *result);
        
        DECLSPEC_XFGVIRT(__x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics, StringsAreNotEqual)
        HRESULT ( STDMETHODCALLTYPE *StringsAreNotEqual )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This,
            /* [in] */ HSTRING expected,
            /* [in] */ HSTRING actual,
            /* [retval][out] */ boolean *result);
        
        DECLSPEC_XFGVIRT(__x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics, StringNotEmpty)
        HRESULT ( STDMETHODCALLTYPE *StringNotEmpty )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This,
            /* [in] */ HSTRING value,
            /* [retval][out] */ boolean *result);
        
        DECLSPEC_XFGVIRT(__x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics, StringOrEmptyIfPlaceholder)
        HRESULT ( STDMETHODCALLTYPE *StringOrEmptyIfPlaceholder )( 
            __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics * This,
            /* [in] */ HSTRING placeholder,
            /* [in] */ HSTRING value,
            /* [retval][out] */ HSTRING *result);
        
        END_INTERFACE
    } __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStaticsVtbl;

    interface __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics
    {
        CONST_VTBL struct __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStaticsVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_GetIids(This,iidCount,iids)	\
    ( (This)->lpVtbl -> GetIids(This,iidCount,iids) ) 

#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_GetRuntimeClassName(This,className)	\
    ( (This)->lpVtbl -> GetRuntimeClassName(This,className) ) 

#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_GetTrustLevel(This,trustLevel)	\
    ( (This)->lpVtbl -> GetTrustLevel(This,trustLevel) ) 


#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_InvertBoolean(This,value,result)	\
    ( (This)->lpVtbl -> InvertBoolean(This,value,result) ) 

#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_PercentageToPercentageValue(This,value,result)	\
    ( (This)->lpVtbl -> PercentageToPercentageValue(This,value,result) ) 

#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_PercentageValueToPercentage(This,value,result)	\
    ( (This)->lpVtbl -> PercentageValueToPercentage(This,value,result) ) 

#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_StringsAreNotEqual(This,expected,actual,result)	\
    ( (This)->lpVtbl -> StringsAreNotEqual(This,expected,actual,result) ) 

#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_StringNotEmpty(This,value,result)	\
    ( (This)->lpVtbl -> StringNotEmpty(This,value,result) ) 

#define __x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_StringOrEmptyIfPlaceholder(This,placeholder,value,result)	\
    ( (This)->lpVtbl -> StringOrEmptyIfPlaceholder(This,placeholder,value,result) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* ____x_ABI_CMicrosoft_CTerminal_CUI_CIConvertersStatics_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

unsigned long             __RPC_USER  HSTRING_UserSize(     unsigned long *, unsigned long            , HSTRING * ); 
unsigned char * __RPC_USER  HSTRING_UserMarshal(  unsigned long *, unsigned char *, HSTRING * ); 
unsigned char * __RPC_USER  HSTRING_UserUnmarshal(unsigned long *, unsigned char *, HSTRING * ); 
void                      __RPC_USER  HSTRING_UserFree(     unsigned long *, HSTRING * ); 

unsigned long             __RPC_USER  HSTRING_UserSize64(     unsigned long *, unsigned long            , HSTRING * ); 
unsigned char * __RPC_USER  HSTRING_UserMarshal64(  unsigned long *, unsigned char *, HSTRING * ); 
unsigned char * __RPC_USER  HSTRING_UserUnmarshal64(unsigned long *, unsigned char *, HSTRING * ); 
void                      __RPC_USER  HSTRING_UserFree64(     unsigned long *, HSTRING * ); 

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


